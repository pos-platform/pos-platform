using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders;

/// <summary>
/// Online buyurtma narx qoidalari (promokod, bonus, yetkazish hududi, chegirmani savdo qatorlariga taqsimlash).
/// Mijoz (quote/yaratish) va xodim (yig'ish/"Tayyor") servislari shu yerdan foydalanadi — bitta mantiq.
/// Tenant filtri yo'q joyda ham ishlashi uchun so'rovlar IgnoreQueryFilters + qo'lda OrganizationId bilan.
/// </summary>
public static class OnlineOrderPricing
{
    public sealed record PromoCheck(PromoCode? Promo, decimal Discount, string? Message);

    public static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    /// <summary>
    /// Promokodni tekshiradi. Kod bo'sh — (null, 0, null). Yaroqsiz — (null, 0, sabab).
    /// shopUserId berilsa — bitta mijozga limit ham tekshiriladi.
    /// </summary>
    public static async Task<PromoCheck> CheckPromoAsync(IApplicationDbContext db, Guid orgId, string? code,
        decimal itemsTotal, Guid? shopUserId, CancellationToken ct)
    {
        var normalized = NormalizeCode(code);
        if (normalized is null) return new PromoCheck(null, 0, null);

        var promo = await db.PromoCodes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Code == normalized, ct);

        var now = DateTime.UtcNow;
        string? error = null;
        if (promo is null || !promo.IsActive)
            error = "Promokod topilmadi yoki faol emas.";
        else if (promo.ValidFrom is { } from && now < from)
            error = "Promokod hali amal qilmaydi.";
        else if (promo.ValidTo is { } to && now > to)
            error = "Promokod muddati tugagan.";
        else if (promo.MaxUses is { } max && promo.UsedCount >= max)
            error = "Promokod limiti tugagan.";
        else if (itemsTotal < promo.MinOrderAmount)
            error = $"Promokod {promo.MinOrderAmount:N0} so'mdan boshlab buyurtmaga amal qiladi.";
        else if (shopUserId is { } userId && promo.PerUserLimit > 0)
        {
            var used = await db.OnlineOrders.IgnoreQueryFilters()
                .CountAsync(o => o.ShopUserId == userId && o.PromoCodeId == promo.Id
                                 && o.Status != OnlineOrderStatus.Rejected && o.Status != OnlineOrderStatus.Cancelled, ct);
            if (used >= promo.PerUserLimit)
                error = "Siz bu promokoddan foydalanib bo'lgansiz.";
        }

        if (error is not null) return new PromoCheck(null, 0, error);

        var discount = PromoDiscount(promo!, itemsTotal);
        return discount <= 0
            ? new PromoCheck(null, 0, "Promokod bu buyurtmaga chegirma bermaydi.")
            : new PromoCheck(promo, discount, null);
    }

    /// <summary>Promokod chegirmasi (butun so'm), mahsulotlar summasidan oshmaydi.</summary>
    public static decimal PromoDiscount(PromoCode p, decimal itemsTotal)
    {
        decimal d = p.DiscountPercent is { } pct && pct > 0
            ? Math.Round(itemsTotal * pct / 100m, 0, MidpointRounding.AwayFromZero)
            : p.DiscountAmount ?? 0;
        return Math.Clamp(d, 0, Math.Max(0, itemsTotal));
    }

    /// <summary>Bonus bilan to'lanadigan eng katta summa (butun so'm): ball, foiz chegarasi va qolgan summa bo'yicha.</summary>
    public static decimal BonusCap(OnlineStoreSettings s, decimal itemsTotal, decimal promoDiscount, decimal points)
    {
        if (!s.BonusPayEnabled || s.BonusMaxPercent <= 0 || points <= 0) return 0;
        var byPercent = itemsTotal * Math.Min(100, s.BonusMaxPercent) / 100m;
        var cap = Math.Min(points, Math.Min(byPercent, itemsTotal - promoDiscount));
        return Math.Max(0, Math.Floor(cap));
    }

    /// <summary>Mijozning shu do'kondagi bonus balli (Customer.ShopUserId orqali). Kartochka yo'q — 0.</summary>
    public static async Task<decimal> CustomerPointsAsync(IApplicationDbContext db, Guid orgId, Guid shopUserId,
        CancellationToken ct) =>
        Math.Max(0, await db.Customers.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == orgId && c.ShopUserId == shopUserId && c.IsActive)
            .Select(c => (decimal?)c.LoyaltyPoints)
            .FirstOrDefaultAsync(ct) ?? 0);

    /// <summary>
    /// Umumiy chegirmani qatorlarga summasiga mutanosib taqsimlaydi (2 xona, qoldiq oxirgi qatorga).
    /// Hech bir qator chegirmasi o'z summasidan oshmaydi.
    /// </summary>
    public static decimal[] Distribute(IReadOnlyList<decimal> grosses, decimal total)
    {
        var result = new decimal[grosses.Count];
        var sum = grosses.Sum();
        if (grosses.Count == 0 || total <= 0 || sum <= 0) return result;
        total = Math.Min(total, sum);

        decimal assigned = 0;
        for (var i = 0; i < grosses.Count - 1; i++)
        {
            result[i] = Math.Min(grosses[i], Math.Round(total * grosses[i] / sum, 2, MidpointRounding.ToZero));
            assigned += result[i];
        }

        var last = grosses.Count - 1;
        result[last] = total - assigned;

        // Oxirgi qator summasidan oshsa — ortig'ini boshqa qatorlarga o'tkazamiz.
        if (result[last] > grosses[last])
        {
            var excess = result[last] - grosses[last];
            result[last] = grosses[last];
            for (var i = 0; i < last && excess > 0; i++)
            {
                var room = grosses[i] - result[i];
                var add = Math.Min(room, excess);
                result[i] += add;
                excess -= add;
            }
        }

        return result;
    }

    /// <summary>Yetkazish hududi: (ruxsat, xabar, masofa km). Radius yoki koordinata bo'lmasa — ruxsat.</summary>
    public static (bool Available, string? Message, double? DistanceKm) CheckDeliveryArea(OnlineStoreSettings s,
        Branch branch, double? lat, double? lng)
    {
        double? distance = lat is { } la && lng is { } lo && branch.Latitude is { } bla && branch.Longitude is { } blo
            ? Math.Round(ShopCatalog.ShopCatalogService.HaversineKm(la, lo, bla, blo), 2)
            : null;

        if (s.DeliveryRadiusKm is not { } radius)
            return (true, null, distance);

        if (distance is null)
            return (true, "Manzil xaritada belgilanmagan — yetkazish hududi tekshirilmadi. Do'kon siz bilan bog'lanishi mumkin.", null);

        return distance <= radius
            ? (true, null, distance)
            : (false, $"Manzil yetkazish hududidan tashqarida ({distance:0.#} km, do'kon {radius:0.#} km gacha yetkazadi).", distance);
    }

    /// <summary>
    /// Promokod ishlatilish sonini qaytaradi (bekor/rad etilganda). SaveChanges chaqirmaydi —
    /// buyurtma holati bilan bitta saqlashda yoziladi.
    /// </summary>
    public static async Task ReleasePromoAsync(IApplicationDbContext db, OnlineOrder order, CancellationToken ct)
    {
        if (order.PromoCodeId is not { } id) return;
        var promo = await db.PromoCodes.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (promo is { UsedCount: > 0 })
            promo.UsedCount--;
    }
}
