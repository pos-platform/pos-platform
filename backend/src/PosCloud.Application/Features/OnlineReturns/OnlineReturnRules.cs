using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineReturns;

/// <summary>Online qaytarish qoidalari (mijoz, xodim va kuryer servislari uchun umumiy).</summary>
public static class OnlineReturnRules
{
    /// <summary>Mijoz haqiqatda olgan miqdor: yig'ilgan − eshik oldida rad etilgan.</summary>
    public static decimal Received(OnlineOrderItem i) =>
        Math.Max(0, OnlineOrderRules.ReservedQuantity(i) - i.RejectedQuantity);

    /// <summary>Hali qaytarish so'rovi berish mumkin bo'lgan miqdor.</summary>
    public static decimal Returnable(OnlineOrderItem i) => Math.Max(0, Received(i) - i.ReturnedQuantity);

    /// <summary>Qaytarish so'rovi mumkin: yoqilgan, Delivered, ReturnDays ichida, savdo bor, qaytariladigan miqdor qolgan.</summary>
    public static bool CanReturn(OnlineOrder o, OnlineStoreSettings? s, DateTime nowUtc) =>
        (s?.ReturnsEnabled ?? true)
        && o.Status == OnlineOrderStatus.Delivered
        && o.SaleId is not null
        && o.DeliveredAt is { } delivered
        && delivered.AddDays(Math.Max(0, s?.ReturnDays ?? 3)) >= nowUtc
        && o.Items.Any(i => Returnable(i) > 0);

    /// <summary>Faol (yopilmagan) qaytarish holatlari.</summary>
    public static bool IsOpen(OnlineReturnStatus s) =>
        s is not (OnlineReturnStatus.Refunded or OnlineReturnStatus.Rejected or OnlineReturnStatus.Cancelled);

    /// <summary>
    /// Online buyurtma qatorlari bo'yicha miqdorlarni savdo qatorlariga (mahsulot bo'yicha, qaytarilmagan qoldiq tartibida)
    /// taqsimlaydi — qisman refund so'rovi. Markirovkali tovar savdoda har kod alohida qator bo'lgani uchun ham ishlaydi.
    /// </summary>
    public static RefundRequest MapToSaleItems(Sale sale, IEnumerable<(Guid ProductId, decimal Quantity)> lines, string? reason)
    {
        var request = new RefundRequest { Items = [], Reason = reason };
        var left = sale.Items.ToDictionary(i => i.Id, i => i.Quantity - i.RefundedQuantity);
        foreach (var (productId, quantity) in lines)
        {
            var need = quantity;
            foreach (var si in sale.Items.Where(i => i.ProductId == productId).OrderBy(i => i.CreatedAt))
            {
                if (need <= 0) break;
                var take = Math.Min(left[si.Id], need);
                if (take <= 0) continue;
                left[si.Id] -= take;
                need -= take;
                var existing = request.Items.FirstOrDefault(x => x.SaleItemId == si.Id);
                if (existing is null) request.Items.Add(new RefundItemRequest { SaleItemId = si.Id, Quantity = take });
                else existing.Quantity += take;
            }
            if (need > 0)
                throw new Common.Exceptions.ValidationException("Qaytariladigan miqdor savdodagi miqdordan oshib ketdi.");
        }
        return request;
    }
}
