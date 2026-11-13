using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders;

/// <summary>
/// Online buyurtma cashback'i: mijoz buyurtmani qabul qilganda (Delivered) yoziladi — "Tayyor"da emas.
/// Xodim (OnlineOrderService.DeliveredAsync) va kuryer (CourierService.DeliveredAsync) yo'llari shu yerdan foydalanadi.
/// Savdo keyin qaytarilsa — SaleService.RefundAsync Sale.LoyaltyPointsEarned bo'yicha ballni qaytarib oladi.
/// </summary>
public static class OnlineOrderCashback
{
    /// <summary>
    /// Buyurtma savdosi bo'yicha Earn (Source=Online) yozadi va Sale.LoyaltyPointsEarned'ni yangilaydi.
    /// Idempotent: shu savdo bo'yicha Earn allaqachon bo'lsa — qayta yozilmaydi. SaveChanges chaqirmaydi.
    /// Yozilgan (yoki avval yozilgan) ballni qaytaradi.
    /// </summary>
    public static async Task<decimal> EarnOnDeliveredAsync(IApplicationDbContext db, OnlineOrder order, CancellationToken ct)
    {
        if (order.SaleId is not { } saleId) return 0;

        var sale = await db.Sales.IgnoreQueryFilters()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct);
        if (sale is null || sale.Status == SaleStatus.Refunded || sale.CustomerId is not { } customerId) return 0;

        // Ikki marta yozilmasin (takroriy so'rov yoki eski — "Tayyor"da yozilgan savdo).
        if (sale.LoyaltyPointsEarned > 0
            || await db.LoyaltyTransactions.IgnoreQueryFilters()
                .AnyAsync(t => t.SaleId == saleId && t.Type == LoyaltyTransactionType.Earn, ct))
            return sale.LoyaltyPointsEarned;

        var customer = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer is null) return 0;

        var loyalty = await LoyaltyRules.LoadAsync(db, customer.OrganizationId, ct);
        var lines = sale.Items
            .Where(i => i.Product is not null)
            .Select(i => new LoyaltyEarnLine(i.Product!.CategoryId, i.Product.ProductType, i.LineTotal,
                i.DiscountAmount > 0 || i.UnitPrice < i.Product.Price));

        // Daraja (Silver/Gold...) foizi — kassadagi savdo bilan bir xil.
        var tier = await LoyaltyRules.TierStatusAsync(db, loyalty, customer.Id, ct);
        var points = LoyaltyRules.ComputeEarn(loyalty, sale.TotalAmount, lines, tier.Tier?.CashbackPercent ?? 0);
        if (points <= 0) return 0;

        var earned = LoyaltyRules.Apply(db, customer, points, LoyaltyTransactionType.Earn, LoyaltySource.Online,
            sale.TotalAmount, $"Online #{order.Number} — cashback (savdo {sale.ReceiptNumber})", sale);
        sale.LoyaltyPointsEarned = earned;
        return earned;
    }
}
