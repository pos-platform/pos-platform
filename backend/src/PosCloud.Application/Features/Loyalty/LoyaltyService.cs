using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Loyalty.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty;

/// <summary>Xodim tomoni: bonus sozlamalari, QR kartani tanish, mijoz tarixi va hisobot.</summary>
public interface ILoyaltyService
{
    Task<LoyaltySettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<LoyaltySettingsDto> UpdateSettingsAsync(UpdateLoyaltySettingsRequest request, CancellationToken ct = default);
    Task<ResolveBonusCardResponse> ResolveAsync(string code, CancellationToken ct = default);
    Task<PagedResult<LoyaltyHistoryItemDto>> GetCustomerHistoryAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default);
    Task<LoyaltyReportDto> GetReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}

public class LoyaltyService : ILoyaltyService
{
    public const string ExpiredCodeMessage = "Bonus karta kodi eskirgan — mijoz ilovada QR'ni yangilasin";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILoyaltyQrSigner _qr;

    public LoyaltyService(IApplicationDbContext db, ICurrentUser currentUser, ILoyaltyQrSigner qr)
    {
        _db = db;
        _currentUser = currentUser;
        _qr = qr;
    }

    private Guid OrgId => _currentUser.OrganizationId
        ?? throw new ValidationException("Tashkilot aniqlanmadi.");

    public async Task<LoyaltySettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var orgId = OrgId;
        var s = await LoyaltyRules.LoadAsync(_db, orgId, ct);
        var names = await _db.Categories.AsNoTracking()
            .Where(c => c.OrganizationId == orgId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return new LoyaltySettingsDto
        {
            Enabled = s.Enabled,
            CashbackPercent = s.CashbackPercent,
            MinSaleAmount = s.MinSaleAmount,
            MaxRedeemPercent = s.MaxRedeemPercent,
            PointsExpireDays = s.PointsExpireDays,
            SignupBonus = s.SignupBonus,
            EarnOnDiscountedItems = s.EarnOnDiscountedItems,
            CategoryRates = s.CategoryRates
                .Select(r => new LoyaltyCategoryRateDto
                {
                    CategoryId = r.Key,
                    CategoryName = names.GetValueOrDefault(r.Key, string.Empty),
                    Percent = r.Value
                })
                .OrderBy(r => r.CategoryName)
                .ToList()
        };
    }

    public async Task<LoyaltySettingsDto> UpdateSettingsAsync(UpdateLoyaltySettingsRequest r, CancellationToken ct = default)
    {
        var orgId = OrgId;

        if (r.CashbackPercent is < 0 or > 100) throw new ValidationException("Cashback foizi 0..100 oralig'ida bo'lishi kerak.");
        if (r.MaxRedeemPercent is < 0 or > 100) throw new ValidationException("Ball bilan to'lash foizi 0..100 oralig'ida bo'lishi kerak.");
        if (r.MinSaleAmount < 0) throw new ValidationException("Minimal savdo summasi manfiy bo'lmasligi kerak.");
        if (r.SignupBonus < 0) throw new ValidationException("Ro'yxatdan o'tish bonusi manfiy bo'lmasligi kerak.");
        if (r.PointsExpireDays is { } days && days is < 30 or > 3650)
            throw new ValidationException("Ball muddati 30..3650 kun yoki bo'sh (muddatsiz) bo'lishi kerak.");

        var rates = r.CategoryRates ?? new();
        if (rates.Any(x => x.Percent is < 0 or > 100))
            throw new ValidationException("Kategoriya foizi 0..100 oralig'ida bo'lishi kerak.");
        if (rates.Select(x => x.CategoryId).Distinct().Count() != rates.Count)
            throw new ValidationException("Kategoriya ro'yxatda takrorlangan.");

        var categoryIds = rates.Select(x => x.CategoryId).ToList();
        var known = await _db.Categories.Where(c => c.OrganizationId == orgId && categoryIds.Contains(c.Id))
            .Select(c => c.Id).ToListAsync(ct);
        if (known.Count != categoryIds.Count)
            throw new ValidationException("Kategoriya topilmadi.");

        var settings = await _db.LoyaltySettings.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);
        if (settings is null)
        {
            settings = new LoyaltySettings { OrganizationId = orgId };
            _db.LoyaltySettings.Add(settings);
        }

        settings.Enabled = r.Enabled;
        settings.CashbackPercent = r.CashbackPercent;
        settings.MinSaleAmount = r.MinSaleAmount;
        settings.MaxRedeemPercent = r.MaxRedeemPercent;
        settings.PointsExpireDays = r.PointsExpireDays;
        settings.SignupBonus = r.SignupBonus;
        settings.EarnOnDiscountedItems = r.EarnOnDiscountedItems;

        // categoryRates — to'liq ro'yxat sifatida almashtiriladi.
        var existing = await _db.LoyaltyCategoryRates.Where(x => x.OrganizationId == orgId).ToListAsync(ct);
        foreach (var old in existing.Where(e => rates.All(x => x.CategoryId != e.CategoryId)))
            _db.LoyaltyCategoryRates.Remove(old);
        foreach (var rate in rates)
        {
            var row = existing.FirstOrDefault(e => e.CategoryId == rate.CategoryId);
            if (row is null)
                _db.LoyaltyCategoryRates.Add(new LoyaltyCategoryRate
                {
                    OrganizationId = orgId,
                    CategoryId = rate.CategoryId,
                    Percent = rate.Percent
                });
            else
                row.Percent = rate.Percent;
        }

        await _db.SaveChangesAsync(ct);
        return await GetSettingsAsync(ct);
    }

    public async Task<ResolveBonusCardResponse> ResolveAsync(string code, CancellationToken ct = default)
    {
        var orgId = OrgId;

        // Imzo, muddat va tashkilot tekshiriladi. Kod hech qachon loglanmaydi.
        if (!_qr.TryVerify(code, out var shopUserId, out var codeOrgId, out var expiresAt)
            || expiresAt < DateTime.UtcNow)
            throw new ValidationException(ExpiredCodeMessage);
        if (codeOrgId != orgId)
            throw new ValidationException("Bu bonus karta boshqa do'konga tegishli.");

        var user = await _db.ShopUsers.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == shopUserId, ct);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
            throw new ValidationException("Mijoz akkaunti topilmadi yoki o'chirilgan.");

        var customer = await ShopCustomerLinker.EnsureAsync(_db, orgId, user, LoyaltySource.Store, ct);
        if (!customer.IsActive)
            throw new ValidationException("Mijoz kartochkasi faol emas.");
        await _db.SaveChangesAsync(ct);

        var settings = await LoyaltyRules.LoadAsync(_db, orgId, ct);
        return new ResolveBonusCardResponse
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            MaskedPhone = LoyaltyRules.MaskPhone(customer.Phone),
            Points = Math.Floor(customer.LoyaltyPoints),
            MaxRedeemPercent = settings.MaxRedeemPercent
        };
    }

    public async Task<PagedResult<LoyaltyHistoryItemDto>> GetCustomerHistoryAsync(Guid customerId, int page, int pageSize,
        CancellationToken ct = default)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new NotFoundException($"Mijoz topilmadi: {customerId}");

        return await LoyaltyHistory.PageAsync(
            _db.LoyaltyTransactions.AsNoTracking().Where(t => t.CustomerId == customerId), page, pageSize, ct);
    }

    public async Task<LoyaltyReportDto> GetReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var tx = _db.LoyaltyTransactions.AsNoTracking().Where(t => t.OrganizationId == orgId);

        // Sana chegaralari UTC kalendar kuni; "to" kuni ham kiradi (ReportService konvensiyasi).
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            tx = tx.Where(t => t.CreatedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            tx = tx.Where(t => t.CreatedAt < toUtc);
        }

        var byType = await tx.GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Sum = g.Sum(t => t.Points) })
            .ToListAsync(ct);
        decimal Sum(LoyaltyTransactionType type) => byType.Where(x => x.Type == type).Sum(x => x.Sum);

        var active = await tx.Select(t => t.CustomerId).Distinct().CountAsync(ct);
        var outstanding = await _db.Customers.AsNoTracking()
            .Where(c => c.OrganizationId == orgId && c.IsActive && c.LoyaltyPoints > 0)
            .SumAsync(c => (decimal?)c.LoyaltyPoints, ct) ?? 0;

        return new LoyaltyReportDto
        {
            // Topilgan: xarid cashback'i + ro'yxatdan o'tish bonusi.
            Earned = Sum(LoyaltyTransactionType.Earn) + Sum(LoyaltyTransactionType.Signup),
            Redeemed = -Sum(LoyaltyTransactionType.Redeem),
            Expired = -Sum(LoyaltyTransactionType.Expire),
            ActiveCustomers = active,
            OutstandingPoints = outstanding
        };
    }
}

/// <summary>Bonus tarixi sahifalash (admin va mijoz uchun umumiy).</summary>
public static class LoyaltyHistory
{
    public const int MaxPageSize = 200;

    public static async Task<PagedResult<LoyaltyHistoryItemDto>> PageAsync(IQueryable<LoyaltyTransaction> query,
        int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new LoyaltyHistoryItemDto
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                Type = t.Type,
                Source = t.Source,
                Points = t.Points,
                Amount = t.Amount,
                Comment = t.Comment,
                ReceiptNumber = t.Sale != null ? t.Sale.ReceiptNumber : null
            })
            .ToListAsync(ct);

        return new PagedResult<LoyaltyHistoryItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}

/// <summary>
/// Ball muddati: pointsExpireDays bor tashkilotlarda oxirgi harakati N kundan eski va balansi musbat mijozlar ballini yondiradi.
/// Idempotent — yondirilgan mijoz balansi 0 bo'ladi va qayta tanlanmaydi.
/// </summary>
public static class LoyaltyExpiry
{
    private const int BatchSize = 500;

    public static async Task<int> RunAsync(IApplicationDbContext db, DateTime nowUtc, CancellationToken ct)
    {
        var orgs = await db.LoyaltySettings.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.PointsExpireDays != null && s.OrganizationId != null)
            .Select(s => new { OrgId = s.OrganizationId!.Value, Days = s.PointsExpireDays!.Value })
            .ToListAsync(ct);

        var total = 0;
        foreach (var org in orgs)
        {
            var cutoff = nowUtc.AddDays(-org.Days);
            while (true)
            {
                var batch = await db.Customers.IgnoreQueryFilters()
                    .Where(c => c.OrganizationId == org.OrgId && c.LoyaltyPoints > 0
                                && (c.LastActivityAt ?? c.CreatedAt) < cutoff)
                    .OrderBy(c => c.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);
                if (batch.Count == 0) break;

                foreach (var c in batch)
                    LoyaltyRules.Apply(db, c, -c.LoyaltyPoints, LoyaltyTransactionType.Expire, LoyaltySource.Store, 0,
                        $"{org.Days} kun faolsizlik — ball muddati tugadi");

                await db.SaveChangesAsync(ct);
                total += batch.Count;
                if (batch.Count < BatchSize) break;
            }
        }
        return total;
    }
}
