using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Server (PosCloud.Application.Features.Promotions.PromotionCalculator + SaleService) bilan
/// AYNAN bir xil aksiya hisobi: kassa ko'rsatgan jami, karta summasi, qaytim va chek
/// server yozadigan savdo bilan teng bo'lishi uchun.
///   • Mos aksiya: aniq mahsulotga, uning kategoriyasiga yoki barchaga (ikkalasi null).
///   • Bir nechta mos kelsa — eng katta foiz.
///   • gross = Round2(narx × miqdor); chegirma = Round2(gross × foiz / 100); qator = Round2(gross − chegirma).
///   • Round2 — 2 xona, MidpointRounding.AwayFromZero.
/// </summary>
public static class PromotionCalculator
{
    private static List<PromotionDto> _active = new();

    /// <summary>Joriy aksiyalar ro'yxati (serverdan yoki oflayn keshdan).</summary>
    public static IReadOnlyList<PromotionDto> Active => _active;

    public static void SetActive(IEnumerable<PromotionDto> promotions) => _active = promotions.ToList();

    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Hozir amal qiladigan eng katta chegirma foizi (aksiya sanasi kassa vaqtida tekshiriladi).</summary>
    public static decimal GetDiscountPercent(Guid productId, Guid categoryId)
    {
        var now = DateTime.UtcNow;
        decimal best = 0m;
        foreach (var p in _active)
        {
            if (!p.IsActive || p.StartDate > now || p.EndDate < now) continue;
            var applies = p.ProductId == productId
                || p.CategoryId == categoryId
                || (p.ProductId is null && p.CategoryId is null);
            if (applies && p.DiscountPercent > best) best = p.DiscountPercent;
        }
        return best;
    }

    public static decimal Gross(ProductDto product, decimal quantity) => Round2(product.Price * quantity);

    public static decimal LineDiscount(ProductDto product, decimal quantity)
    {
        var percent = GetDiscountPercent(product.Id, product.CategoryId);
        return percent <= 0 ? 0m : Round2(Gross(product, quantity) * percent / 100m);
    }

    public static decimal LineTotal(ProductDto product, decimal quantity)
        => Round2(Gross(product, quantity) - LineDiscount(product, quantity));
}
