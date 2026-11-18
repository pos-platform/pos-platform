using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Features.Loyalty.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty;

/// <summary>Bosqich 10.2: sozlamalar o'zgarishlari tarixi, bonus harakatlari jurnali, qo'lda tuzatish.</summary>
public partial class LoyaltyService
{
    private const int JournalMaxPageSize = 200;

    public async Task<LoyaltyPageDto<LoyaltySettingsChangeDto>> GetSettingsHistoryAsync(int page, int pageSize,
        CancellationToken ct = default)
    {
        var orgId = OrgId;
        (page, pageSize) = Normalize(page, pageSize);
        var query = _db.LoyaltySettingsChanges.AsNoTracking().Where(c => c.OrganizationId == orgId);

        return new LoyaltyPageDto<LoyaltySettingsChangeDto>
        {
            Total = await query.CountAsync(ct),
            Items = await query
                .OrderByDescending(c => c.ChangedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new LoyaltySettingsChangeDto
                {
                    ChangedAt = c.ChangedAt,
                    UserName = c.UserName,
                    Field = c.Field,
                    OldValue = c.OldValue,
                    NewValue = c.NewValue
                })
                .ToListAsync(ct)
        };
    }

    public async Task<LoyaltyTransactionsPageDto> GetTransactionsAsync(LoyaltyTransactionsQuery q, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var (page, pageSize) = Normalize(q.Page, q.PageSize);

        var tx = _db.LoyaltyTransactions.AsNoTracking().Where(t => t.OrganizationId == orgId);

        // Sana chegaralari UTC kalendar kuni; "to" kuni ham kiradi.
        if (q.From is { } from)
        {
            var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
            tx = tx.Where(t => t.CreatedAt >= fromUtc);
        }
        if (q.To is { } to)
        {
            var toUtc = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
            tx = tx.Where(t => t.CreatedAt < toUtc);
        }
        if (q.CustomerId is { } customerId)
            tx = tx.Where(t => t.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            var digits = new string(s.Where(char.IsDigit).ToArray());
            tx = tx.Where(t => t.Customer!.Name.ToLower().Contains(s)
                               || (digits.Length >= 3 && t.Customer.Phone != null && t.Customer.Phone.Contains(digits))
                               || (t.Sale != null && t.Sale.ReceiptNumber.ToLower().Contains(s)));
        }

        // Jami (tur filtridan oldin — barcha turlar bo'yicha yig'indi).
        var byType = await tx.GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Sum = g.Sum(t => t.Points) })
            .ToListAsync(ct);
        decimal Sum(LoyaltyTransactionType type) => byType.Where(x => x.Type == type).Sum(x => x.Sum);

        if (q.Type is { } type)
            tx = tx.Where(t => t.Type == type);

        var total = await tx.CountAsync(ct);
        var rows = await tx
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.CreatedAt,
                t.CustomerId,
                CustomerName = t.Customer!.Name,
                CustomerPhone = t.Customer.Phone,
                t.Type,
                t.Points,
                t.BalanceAfter,
                t.Source,
                t.SaleId,
                ReceiptNumber = t.Sale != null ? t.Sale.ReceiptNumber : null,
                OnlineOrderId = t.SaleId != null
                    ? _db.OnlineOrders.Where(o => o.SaleId == t.SaleId).Select(o => (Guid?)o.Id).FirstOrDefault()
                    : null,
                t.Comment,
                t.UserName
            })
            .ToListAsync(ct);

        return new LoyaltyTransactionsPageDto
        {
            Total = total,
            Items = rows.Select(r => new LoyaltyTransactionDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                CustomerPhone = LoyaltyRules.MaskPhone(r.CustomerPhone),
                Type = r.Type,
                Points = r.Points,
                BalanceAfter = r.BalanceAfter,
                Source = r.Source,
                SaleId = r.SaleId,
                ReceiptNumber = r.ReceiptNumber,
                OnlineOrderId = r.OnlineOrderId,
                Note = r.Comment,
                UserName = r.UserName
            }).ToList(),
            Totals = new LoyaltyTransactionTotalsDto
            {
                Earned = Sum(LoyaltyTransactionType.Earn) + Sum(LoyaltyTransactionType.Signup) + Sum(LoyaltyTransactionType.Referral),
                Redeemed = -Sum(LoyaltyTransactionType.Redeem),
                Refunded = Sum(LoyaltyTransactionType.Refund),
                Expired = -Sum(LoyaltyTransactionType.Expire),
                Adjusted = Sum(LoyaltyTransactionType.Adjust)
            }
        };
    }

    public async Task<LoyaltyAdjustResultDto> AdjustAsync(Guid customerId, LoyaltyAdjustRequest request, CancellationToken ct = default)
    {
        var orgId = OrgId;
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new ValidationException("Tuzatish sababini kiriting.");
        if (request.Points == 0 || request.Points != Math.Floor(request.Points) || Math.Abs(request.Points) > 100_000_000)
            throw new ValidationException("Ball butun son va 0 dan farqli bo'lishi kerak.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.OrganizationId == orgId, ct)
                       ?? throw new NotFoundException($"Mijoz topilmadi: {customerId}");

        var applied = LoyaltyRules.Apply(_db, customer, request.Points, LoyaltyTransactionType.Adjust, LoyaltySource.Store, 0,
            reason, userId: _currentUser.UserId, userName: _currentUser.Username);
        if (applied == 0)
            throw new ValidationException("Mijoz balansida ayiriladigan ball yo'q.");
        await _db.SaveChangesAsync(ct);

        return new LoyaltyAdjustResultDto { CustomerId = customer.Id, Points = applied, Balance = customer.LoyaltyPoints };
    }

    /// <summary>PUT /api/loyalty/settings: har o'zgargan maydon uchun tarix yozuvi (SaveChanges chaqirmaydi).</summary>
    private async Task RecordSettingsChangesAsync(Guid orgId, LoyaltySettingsDto before, UpdateLoyaltySettingsRequest r,
        List<LoyaltyTierDto>? tiers, CancellationToken ct)
    {
        var changes = new List<(string Field, string? Old, string? New)>();
        void Cmp(string field, string? oldV, string? newV)
        {
            if (!string.Equals(oldV, newV, StringComparison.Ordinal)) changes.Add((field, oldV, newV));
        }

        Cmp("enabled", Fmt(before.Enabled), Fmt(r.Enabled));
        Cmp("cashbackPercent", Fmt(before.CashbackPercent), Fmt(r.CashbackPercent));
        Cmp("minSaleAmount", Fmt(before.MinSaleAmount), Fmt(r.MinSaleAmount));
        Cmp("maxRedeemPercent", Fmt(before.MaxRedeemPercent), Fmt(r.MaxRedeemPercent));
        Cmp("pointsExpireDays", before.PointsExpireDays?.ToString(CultureInfo.InvariantCulture),
            r.PointsExpireDays?.ToString(CultureInfo.InvariantCulture));
        Cmp("signupBonus", Fmt(before.SignupBonus), Fmt(r.SignupBonus));
        Cmp("earnOnDiscountedItems", Fmt(before.EarnOnDiscountedItems), Fmt(r.EarnOnDiscountedItems));

        // Kategoriya stavkalari: category:<nom>.
        var newRates = (r.CategoryRates ?? new()).ToDictionary(x => x.CategoryId, x => x.Percent);
        var ids = newRates.Keys.Concat(before.CategoryRates.Select(x => x.CategoryId)).Distinct().ToList();
        var names = await _db.Categories.AsNoTracking()
            .Where(c => c.OrganizationId == orgId && ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        foreach (var id in ids)
        {
            var oldRate = before.CategoryRates.FirstOrDefault(x => x.CategoryId == id)?.Percent;
            decimal? newRate = newRates.TryGetValue(id, out var p) ? p : null;
            Cmp($"category:{names.GetValueOrDefault(id, id.ToString())}",
                oldRate is { } o ? Fmt(o) + "%" : null, newRate is { } n ? Fmt(n) + "%" : null);
        }

        // Darajalar (berilgan bo'lsa): tier:<nom> → "minSpend / foiz%".
        if (tiers is not null)
        {
            static string TierValue(LoyaltyTierDto t) => $"{Fmt(t.MinSpend)} / {Fmt(t.CashbackPercent)}%";
            var oldByName = before.Tiers.ToDictionary(t => t.Name.ToLowerInvariant(), t => t);
            var newByName = tiers.ToDictionary(t => t.Name.ToLowerInvariant(), t => t);
            foreach (var key in oldByName.Keys.Union(newByName.Keys))
            {
                oldByName.TryGetValue(key, out var o);
                newByName.TryGetValue(key, out var n);
                Cmp($"tier:{(n ?? o)!.Name}", o is null ? null : TierValue(o), n is null ? null : TierValue(n));
            }
        }

        var now = DateTime.UtcNow;
        foreach (var (field, oldV, newV) in changes)
            _db.LoyaltySettingsChanges.Add(new LoyaltySettingsChange
            {
                OrganizationId = orgId,
                ChangedAt = now,
                UserId = _currentUser.UserId,
                UserName = Cut(_currentUser.Username, 100),
                Field = Cut(field, 100)!,
                OldValue = Cut(oldV, 200),
                NewValue = Cut(newV, 200)
            });
    }

    private static string Fmt(decimal v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Fmt(bool v) => v ? "true" : "false";
    private static string? Cut(string? s, int max) => s is { Length: > 0 } && s.Length > max ? s[..max] : s;

    private static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, JournalMaxPageSize));
}
