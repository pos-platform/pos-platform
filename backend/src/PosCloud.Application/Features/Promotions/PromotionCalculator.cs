using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Promotions;

/// <summary>
/// Mahsulot uchun amaldagi aksiyalardan eng yaxshi (eng katta) chegirma foizini topadi.
/// Mos keladigan aksiya: aniq mahsulotga, uning kategoriyasiga, yoki barchaga.
/// </summary>
public static class PromotionCalculator
{
    public static decimal GetDiscountPercent(
        IEnumerable<Promotion> activePromotions,
        Guid productId,
        Guid categoryId)
    {
        decimal best = 0m;

        foreach (var promo in activePromotions)
        {
            var applies =
                promo.ProductId == productId ||
                promo.CategoryId == categoryId ||
                (promo.ProductId is null && promo.CategoryId is null);

            if (applies && promo.DiscountPercent > best)
            {
                best = promo.DiscountPercent;
            }
        }

        return best;
    }

    public static bool IsActiveAt(Promotion p, DateTime nowUtc) =>
        p.IsActive && p.StartDate <= nowUtc && p.EndDate >= nowUtc;
}
