using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders;

/// <summary>
/// Online buyurtma bo'yicha umumiy qoidalar (mijoz va xodim servislari uchun bitta joyda).
/// Ombor rezervi: buyurtma qabul qilinganda (Accepted) band qilinadi, "Ready"da Sale ombordan
/// ayirganda yoki bekor/rad etilganda bo'shatiladi.
/// </summary>
public static class OnlineOrderRules
{
    public static readonly OnlineOrderStatus[] FinishedStatuses =
        [OnlineOrderStatus.Delivered, OnlineOrderStatus.Rejected, OnlineOrderStatus.Cancelled];

    public static bool IsFinished(OnlineOrderStatus s) => FinishedStatuses.Contains(s);

    /// <summary>Mijoz faqat yig'ish boshlanmaguncha bekor qila oladi.</summary>
    public static bool CustomerCanCancel(OnlineOrderStatus s) =>
        s is OnlineOrderStatus.New or OnlineOrderStatus.Accepted;

    /// <summary>Rezerv shu holatlarda ushlab turiladi.</summary>
    public static bool HoldsReservation(OnlineOrderStatus s) =>
        s is OnlineOrderStatus.Accepted or OnlineOrderStatus.Picking;

    /// <summary>Qator uchun band qilinadigan miqdor.</summary>
    public static decimal ReservedQuantity(OnlineOrderItem i) => i.IsRemoved ? 0 : i.PickedQuantity ?? i.Quantity;

    /// <summary>Buyurtma qatorlari bo'yicha rezervni oshiradi (sign = +1) yoki bo'shatadi (sign = -1).</summary>
    public static async Task AdjustReservationAsync(IApplicationDbContext db, OnlineOrder order, int sign,
        CancellationToken ct)
    {
        var perProduct = order.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(ReservedQuantity) })
            .Where(x => x.Qty != 0)
            .ToList();
        if (perProduct.Count == 0) return;

        var ids = perProduct.Select(x => x.ProductId).ToList();
        var stocks = await db.BranchStocks.IgnoreQueryFilters()
            .Where(s => s.BranchId == order.BranchId && ids.Contains(s.ProductId))
            .ToListAsync(ct);

        foreach (var x in perProduct)
        {
            var stock = stocks.FirstOrDefault(s => s.ProductId == x.ProductId);
            if (stock is null) continue;
            stock.Reserved = Math.Max(0, stock.Reserved + sign * x.Qty);
        }
    }
}
