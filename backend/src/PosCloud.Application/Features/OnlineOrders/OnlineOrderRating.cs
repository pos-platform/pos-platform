using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders;

/// <summary>Buyurtma bahosi qoidalari (mijoz ilovasi va xodim hisobotida umumiy).</summary>
public static class OnlineOrderRating
{
    /// <summary>Yetkazilgandan keyin shuncha kun ichida baholash mumkin.</summary>
    public const int RateWindowDays = 14;

    public static readonly string[] AllowedTags =
        ["fast", "fresh", "polite", "accurate", "late", "damaged", "missing", "wrong_item"];

    public static bool CanRate(OnlineOrder o) =>
        o.Status == OnlineOrderStatus.Delivered
        && o.Rating is null
        && (o.DeliveredAt ?? o.UpdatedAt ?? o.CreatedAt) >= DateTime.UtcNow.AddDays(-RateWindowDays);

    /// <summary>Bazadagi vergul bilan ajratilgan teglar → ro'yxat.</summary>
    public static List<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
