using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopPush;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineMarketing;

/// <summary>Push kampaniya qoidalari: kunlik limit, qabul qiluvchilar.</summary>
public static class PushCampaignRules
{
    /// <summary>O'zbekiston vaqti (UTC+5).</summary>
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(5);

    /// <summary>Mahalliy (UTC+5) kun boshidan beri yuborilgan (yoki yuborilayotgan) kampaniyalar soni.</summary>
    public static async Task<int> SentTodayAsync(IApplicationDbContext db, Guid orgId, DateTime nowUtc, CancellationToken ct)
    {
        var dayStart = (nowUtc + LocalOffset).Date - LocalOffset;
        return await db.ShopPushCampaigns.IgnoreQueryFilters()
            .CountAsync(c => c.OrganizationId == orgId && c.SentAt >= dayStart
                             && (c.Status == PushCampaignStatus.Sending || c.Status == PushCampaignStatus.Sent), ct);
    }

    /// <summary>
    /// Qabul qiluvchilar: tashkilotda Customer.ShopUserId bog'langan yoki buyurtma bergan mijozlar;
    /// filial berilsa — faqat shu filialda buyurtma berganlar. O'chirilgan/faol bo'lmagan akkauntlar chiqmaydi.
    /// </summary>
    public static async Task<List<Guid>> RecipientsAsync(IApplicationDbContext db, Guid orgId, Guid? branchId,
        CancellationToken ct)
    {
        IQueryable<Guid> ids;
        if (branchId is { } b)
        {
            ids = db.OnlineOrders.IgnoreQueryFilters()
                .Where(o => o.OrganizationId == orgId && o.BranchId == b)
                .Select(o => o.ShopUserId);
        }
        else
        {
            var fromCustomers = db.Customers.IgnoreQueryFilters()
                .Where(c => c.OrganizationId == orgId && c.IsActive && c.ShopUserId != null)
                .Select(c => c.ShopUserId!.Value);
            var fromOrders = db.OnlineOrders.IgnoreQueryFilters()
                .Where(o => o.OrganizationId == orgId)
                .Select(o => o.ShopUserId);
            ids = fromCustomers.Union(fromOrders);
        }

        return await db.ShopUsers.IgnoreQueryFilters()
            .Where(u => u.IsActive && u.DeletedAt == null && ids.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);
    }
}

/// <summary>
/// Marketing fon vazifalari (Api'dagi BackgroundService'lar chaqiradi): rejalashtirilgan push kampaniyalar,
/// "yana sotuvda" va savatcha eslatmasi. Tenant filtri yo'q (fon kontekstida tashkilot yo'q) — hammasi aniq filtrlanadi.
/// </summary>
public static class MarketingJobs
{
    private const int CampaignBatch = 10;
    private const int StockBatch = 500;
    private const int CartBatch = 200;
    private static readonly TimeSpan NotifiedKeep = TimeSpan.FromDays(7);
    private static readonly TimeSpan CartMinAge = TimeSpan.FromHours(2);
    private static readonly TimeSpan CartMaxAge = TimeSpan.FromHours(24);

    /// <summary>Vaqti kelgan (Scheduled, ScheduledAt ≤ hozir) kampaniyalarni yuboradi. Yuborilgan kampaniyalar soni.</summary>
    public static async Task<int> SendDueCampaignsAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc,
        CancellationToken ct)
    {
        var due = await db.ShopPushCampaigns.IgnoreQueryFilters()
            .Where(c => c.Status == PushCampaignStatus.Scheduled && c.ScheduledAt != null && c.ScheduledAt <= nowUtc
                        && c.OrganizationId != null)
            .OrderBy(c => c.ScheduledAt)
            .Take(CampaignBatch)
            .ToListAsync(ct);

        var count = 0;
        foreach (var c in due)
        {
            var orgId = c.OrganizationId!.Value;
            if (await PushCampaignRules.SentTodayAsync(db, orgId, nowUtc, ct) >= OnlineMarketingService.MaxCampaignsPerDay)
            {
                // Kunlik limit — yuborilmaydi.
                c.Status = PushCampaignStatus.Failed;
                await db.SaveChangesAsync(ct);
                continue;
            }

            c.Status = PushCampaignStatus.Sending;
            c.SentAt = nowUtc;
            await db.SaveChangesAsync(ct);

            try
            {
                var recipients = await PushCampaignRules.RecipientsAsync(db, orgId, c.BranchId, ct);
                var data = new Dictionary<string, string>
                {
                    ["type"] = "campaign",
                    ["linkType"] = c.LinkType.ToString(),
                    ["linkValue"] = c.LinkValue ?? string.Empty
                };
                if (c.BranchId is { } b) data["branchId"] = b.ToString();

                c.SentCount = await push.SendToUsersAsync(recipients, c.Title, c.Body, data, ct);
                c.Status = PushCampaignStatus.Sent;
                c.SentAt = DateTime.UtcNow;
                count++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                c.Status = PushCampaignStatus.Failed;
            }
            await db.SaveChangesAsync(ct);
        }
        return count;
    }

    /// <summary>"Kelganda xabar berish": qoldiq paydo bo'lgan obunalarga push, NotifiedAt qo'yiladi; eskilari o'chiriladi.</summary>
    public static async Task<int> NotifyBackInStockAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc,
        CancellationToken ct)
    {
        var stocks = db.BranchStocks.IgnoreQueryFilters();
        var subs = await db.ShopStockSubscriptions.IgnoreQueryFilters()
            .Where(s => s.NotifiedAt == null && s.Product!.ShowOnline
                        && stocks.Any(b => b.BranchId == s.BranchId && b.ProductId == s.ProductId && b.Quantity - b.Reserved > 0))
            .OrderBy(s => s.CreatedAt)
            .Take(StockBatch)
            .Select(s => new { Sub = s, ProductName = s.Product!.Name })
            .ToListAsync(ct);

        foreach (var row in subs)
        {
            var s = row.Sub;
            await push.SendToUserAsync(new ShopUserPush(s.ShopUserId,
                $"«{row.ProductName}» yana sotuvda", "Tugashidan oldin buyurtma bering.",
                $"«{row.ProductName}» снова в продаже", "Успейте заказать.",
                new Dictionary<string, string>
                {
                    ["type"] = "back_in_stock",
                    ["productId"] = s.ProductId.ToString(),
                    ["branchId"] = s.BranchId.ToString()
                }), ct);
            s.NotifiedAt = nowUtc;
        }

        // Xabar berilgan obunalar bir haftadan keyin o'chiriladi.
        var keepUntil = nowUtc - NotifiedKeep;
        var old = await db.ShopStockSubscriptions.IgnoreQueryFilters()
            .Where(s => s.NotifiedAt != null && s.NotifiedAt < keepUntil)
            .Take(1000)
            .ToListAsync(ct);
        db.ShopStockSubscriptions.RemoveRange(old);

        if (subs.Count > 0 || old.Count > 0)
            await db.SaveChangesAsync(ct);
        return subs.Count;
    }

    /// <summary>
    /// Savatcha eslatmasi: o'zgarganiga 2–24 soat bo'lgan, keyin buyurtma berilmagan savatchalar; mijozga kuniga ko'pi bilan 1 ta;
    /// faqat 09:00–21:00 (UTC+5).
    /// </summary>
    public static async Task<int> RemindCartsAsync(IApplicationDbContext db, IShopPushService push, DateTime nowUtc,
        CancellationToken ct)
    {
        var localHour = (nowUtc + PushCampaignRules.LocalOffset).Hour;
        if (localHour is < 9 or >= 21) return 0;

        var newest = nowUtc - CartMinAge;
        var oldest = nowUtc - CartMaxAge;
        var dayAgo = nowUtc.AddDays(-1);
        var orders = db.OnlineOrders.IgnoreQueryFilters();
        var snapshots = db.ShopCartSnapshots.IgnoreQueryFilters();

        var due = await snapshots
            .Where(s => s.ItemCount > 0 && s.ChangedAt <= newest && s.ChangedAt >= oldest
                        && (s.RemindedAt == null || (s.RemindedAt < s.ChangedAt && s.RemindedAt < dayAgo))
                        && !orders.Any(o => o.ShopUserId == s.ShopUserId && o.BranchId == s.BranchId && o.CreatedAt >= s.ChangedAt)
                        && !snapshots.Any(o => o.ShopUserId == s.ShopUserId && o.RemindedAt >= dayAgo))
            .OrderBy(s => s.ChangedAt)
            .Take(CartBatch)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var s in due.GroupBy(x => x.ShopUserId).Select(g => g.First()))
        {
            await push.SendToUserAsync(new ShopUserPush(s.ShopUserId,
                $"Savatchangizda {s.ItemCount} ta mahsulot qoldi", "Buyurtmani rasmiylashtirishni unutmang.",
                $"В корзине осталось товаров: {s.ItemCount}", "Не забудьте оформить заказ.",
                new Dictionary<string, string>
                {
                    ["type"] = "cart_reminder",
                    ["branchId"] = s.BranchId.ToString()
                }), ct);
            s.RemindedAt = nowUtc;
            sent++;
        }

        if (sent > 0) await db.SaveChangesAsync(ct);
        return sent;
    }
}
