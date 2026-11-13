using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty;

/// <summary>Tashkilotning amaldagi bonus sozlamalari (yozuv bo'lmasa — standart qiymatlar).</summary>
public sealed record EffectiveLoyaltySettings(
    bool Enabled,
    decimal CashbackPercent,
    decimal MinSaleAmount,
    decimal MaxRedeemPercent,
    int? PointsExpireDays,
    decimal SignupBonus,
    bool EarnOnDiscountedItems,
    IReadOnlyDictionary<Guid, decimal> CategoryRates)
{
    public static EffectiveLoyaltySettings Default { get; } =
        new(true, 1m, 0m, 50m, null, 0m, true, new Dictionary<Guid, decimal>());

    /// <summary>Darajalar (MinSpend o'sish tartibida).</summary>
    public IReadOnlyList<LoyaltyTierRule> Tiers { get; init; } = Array.Empty<LoyaltyTierRule>();
}

/// <summary>Loyalty darajasi qoidasi.</summary>
public sealed record LoyaltyTierRule(Guid Id, string Name, decimal MinSpend, decimal CashbackPercent);

/// <summary>Mijozning joriy darajasi, keyingi darajasi va oxirgi 365 kundagi xaridi.</summary>
public sealed record LoyaltyTierStatus(LoyaltyTierRule? Tier, LoyaltyTierRule? Next, decimal Spend)
{
    public static LoyaltyTierStatus None { get; } = new(null, null, 0);
}

/// <summary>Ball hisoblash uchun savdo qatori.</summary>
public readonly record struct LoyaltyEarnLine(Guid CategoryId, ProductType ProductType, decimal LineTotal, bool IsDiscounted);

/// <summary>
/// Bonus dasturining yagona mantiqi: sozlamalarni o'qish, ball hisoblash (cashback) va balans harakatini yozish.
/// SaleService (kassa, restoran, online "Tayyor"), online buyurtma, admin tuzatish va muddat fon vazifasi shu yerdan foydalanadi.
/// </summary>
public static class LoyaltyRules
{
    /// <summary>Online buyurtmadan yaratilgan savdoning idempotentlik kaliti prefiksi.</summary>
    public const string OnlineSalePrefix = "online:";

    /// <summary>Tashkilot sozlamalari. Tenant filtri chetlab o'tiladi — tashkilot aniq beriladi.</summary>
    public static async Task<EffectiveLoyaltySettings> LoadAsync(IApplicationDbContext db, Guid? organizationId,
        CancellationToken ct)
    {
        if (organizationId is not { } orgId) return EffectiveLoyaltySettings.Default;

        var s = await db.LoyaltySettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId, ct);
        var rates = await db.LoyaltyCategoryRates.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.OrganizationId == orgId)
            .ToDictionaryAsync(r => r.CategoryId, r => r.Percent, ct);

        var tiers = await db.LoyaltyTiers.IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.OrganizationId == orgId)
            .OrderBy(t => t.MinSpend).ThenBy(t => t.SortOrder)
            .Select(t => new LoyaltyTierRule(t.Id, t.Name, t.MinSpend, t.CashbackPercent))
            .ToListAsync(ct);

        var result = s is null
            ? EffectiveLoyaltySettings.Default with { CategoryRates = rates }
            : new EffectiveLoyaltySettings(s.Enabled, s.CashbackPercent, s.MinSaleAmount, s.MaxRedeemPercent,
                s.PointsExpireDays, s.SignupBonus, s.EarnOnDiscountedItems, rates);
        return result with { Tiers = tiers };
    }

    /// <summary>Daraja hisobi uchun davr (kun).</summary>
    public const int TierSpendDays = 365;

    /// <summary>Mijozning oxirgi 365 kundagi xaridi: yakunlangan (qaytarilmagan) savdolar jami. Tenant filtri chetlab o'tiladi.</summary>
    public static async Task<decimal> SpendAsync(IApplicationDbContext db, Guid customerId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-TierSpendDays);
        return await db.Sales.IgnoreQueryFilters()
            .Where(x => x.CustomerId == customerId && x.Type == SaleType.Sale && x.Status == SaleStatus.Completed
                        && x.CreatedAt >= since)
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;
    }

    /// <summary>Xarid summasi bo'yicha daraja: MinSpend ≤ spend bo'lgan eng yuqorisi; keyingisi — birinchi kattasi.</summary>
    public static LoyaltyTierStatus ResolveTier(EffectiveLoyaltySettings s, decimal spend)
    {
        LoyaltyTierRule? tier = null, next = null;
        foreach (var t in s.Tiers.OrderBy(t => t.MinSpend))
        {
            if (t.MinSpend <= spend) tier = t;
            else { next = t; break; }
        }
        return new LoyaltyTierStatus(tier, next, spend);
    }

    /// <summary>Mijoz darajasi (darajalar bo'lmasa yoki dastur o'chiq bo'lsa — bazaga so'rov yuborilmaydi).</summary>
    public static async Task<LoyaltyTierStatus> TierStatusAsync(IApplicationDbContext db, EffectiveLoyaltySettings s,
        Guid customerId, CancellationToken ct)
    {
        if (s.Tiers.Count == 0) return LoyaltyTierStatus.None;
        return ResolveTier(s, await SpendAsync(db, customerId, ct));
    }

    /// <summary>
    /// Savdodan topiladigan ball (butun so'm, pastga yaxlitlanadi). Baza — qatorning chegirmadan keyingi summasi;
    /// Service qatorlari va (sozlamaga ko'ra) chegirmali qatorlar hisobga olinmaydi; jami minSaleAmount'dan kam bo'lsa — 0.
    /// Foiz: kategoriya stavkasi (bo'lmasa bazaviy) va mijoz darajasi foizining eng kattasi.
    /// </summary>
    public static decimal ComputeEarn(EffectiveLoyaltySettings s, decimal saleTotal, IEnumerable<LoyaltyEarnLine> lines,
        decimal tierPercent = 0)
    {
        if (!s.Enabled || saleTotal <= 0 || saleTotal < s.MinSaleAmount) return 0;

        decimal sum = 0;
        foreach (var l in lines)
        {
            if (l.ProductType == ProductType.Service || l.LineTotal <= 0) continue;
            if (!s.EarnOnDiscountedItems && l.IsDiscounted) continue;
            var percent = s.CategoryRates.TryGetValue(l.CategoryId, out var rate) ? rate : s.CashbackPercent;
            percent = Math.Max(percent, tierPercent);
            if (percent <= 0) continue;
            sum += l.LineTotal * percent / 100m;
        }
        return Math.Max(0, Math.Floor(sum));
    }

    /// <summary>Savdo manbai: online buyurtma kaliti bo'lsa — Online, aks holda — Store.</summary>
    public static LoyaltySource SourceFor(string? clientSaleId) =>
        clientSaleId is not null && clientSaleId.StartsWith(OnlineSalePrefix, StringComparison.Ordinal)
            ? LoyaltySource.Online
            : LoyaltySource.Store;

    /// <summary>
    /// Balans harakatini yozadi: balansni o'zgartiradi (0 dan pastga tushmaydi), LastActivityAt'ni yangilaydi va tranzaksiya qo'shadi.
    /// Manfiy harakat balansdan oshsa — balansgacha qisqartiriladi. Haqiqatda yozilgan ballni qaytaradi (0 bo'lsa — yozilmaydi).
    /// </summary>
    public static decimal Apply(IApplicationDbContext db, Customer customer, decimal points, LoyaltyTransactionType type,
        LoyaltySource source, decimal amount, string? comment, Sale? sale = null, Guid? saleId = null)
    {
        if (points < 0 && -points > customer.LoyaltyPoints)
            points = -Math.Max(0, customer.LoyaltyPoints);
        if (points == 0) return 0;

        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints + points);
        customer.LastActivityAt = DateTime.UtcNow;

        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            OrganizationId = customer.OrganizationId,
            CustomerId = customer.Id,
            Points = points,
            Amount = amount,
            Comment = comment,
            Type = type,
            Source = source,
            Sale = sale,
            SaleId = sale is null ? saleId : null
        });
        return points;
    }

    /// <summary>Telefonni yashiradi: +998 ** *** 45 67.</summary>
    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "***";
        var tail = digits[^4..];
        return digits.Length >= 12 && digits.StartsWith("998")
            ? $"+998 ** *** {tail[..2]} {tail[2..]}"
            : $"*** {tail[..2]} {tail[2..]}";
    }
}
