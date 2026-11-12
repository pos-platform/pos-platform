using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopOrders.Dtos;

public class ShopOrderItemRequest
{
    public Guid ProductId { get; set; }

    /// <summary>Dona tovar uchun butun son, tarozi tovari uchun kg (0.001 aniqlik).</summary>
    public decimal Quantity { get; set; }
}

/// <summary>Savatcha hisobi (saqlamaydi). Narx va summa serverda hisoblanadi.</summary>
public class ShopOrderQuoteRequest
{
    public Guid BranchId { get; set; }
    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Delivery;

    [MaxLength(100)]
    public List<ShopOrderItemRequest> Items { get; set; } = [];
}

public class CreateShopOrderRequest : ShopOrderQuoteRequest
{
    /// <summary>Yetkazishda majburiy: mijozning saqlangan manzili.</summary>
    public Guid? AddressId { get; set; }

    /// <summary>Cash, Card (kuryer terminali) yoki Transfer.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>Null — imkon qadar tez. UTC.</summary>
    public DateTime? ScheduledFor { get; set; }

    public bool Contactless { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public OutOfStockPolicy OutOfStockPolicy { get; set; } = OutOfStockPolicy.CallMe;
}

public class CancelShopOrderRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class ShopOrderQuoteLineDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal Available { get; set; }

    /// <summary>Null — muammo yo'q. Aks holda: not_found, out_of_stock, not_enough, invalid_quantity.</summary>
    public string? Problem { get; set; }
}

public class ShopOrderQuoteDto
{
    public Guid BranchId { get; set; }
    public List<ShopOrderQuoteLineDto> Lines { get; set; } = [];
    public decimal ItemsTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MinOrderAmount { get; set; }

    /// <summary>Minimal summagacha yetmayotgan qism (0 — yetarli).</summary>
    public decimal MinOrderShortfall { get; set; }

    /// <summary>Bepul yetkazishgacha qolgan summa. Null — bepul yetkazish yo'q yoki olib ketish.</summary>
    public decimal? FreeDeliveryRemaining { get; set; }
    public decimal? FreeDeliveryFrom { get; set; }

    /// <summary>Buyurtma berish mumkinmi (muammosiz qatorlar, minimal summa, do'kon ochiq).</summary>
    public bool CanOrder { get; set; }

    /// <summary>Umumiy to'siq (do'kon yopiq, usul o'chirilgan) — foydalanuvchiga ko'rsatiladigan matn.</summary>
    public string? Message { get; set; }
}

public class ShopOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PickedQuantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsRemoved { get; set; }
    public Guid? ReplacesItemId { get; set; }
}

public class ShopOrderSummaryDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public OnlineOrderStatus Status { get; set; }
    public FulfillmentType FulfillmentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }

    /// <summary>Ro'yxatdagi kichik rasmlar uchun (ko'pi bilan 3 ta).</summary>
    public List<string?> PreviewImages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class ShopOrderDto : ShopOrderSummaryDto
{
    public string? BranchName { get; set; }
    public string? StorePhone { get; set; }
    public string? BranchAddress { get; set; }

    public string? AddressLine { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public bool Contactless { get; set; }
    public string? Comment { get; set; }
    public OutOfStockPolicy OutOfStockPolicy { get; set; }

    public decimal ItemsTotal { get; set; }
    public decimal DeliveryFee { get; set; }

    public List<ShopOrderItemDto> Items { get; set; } = [];

    public DateTime? AcceptedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }

    /// <summary>Mijoz hali bekor qila oladimi (Picking'gacha).</summary>
    public bool CanCancel { get; set; }

    /// <summary>O'tkazma cheki yuklash mumkinmi.</summary>
    public bool CanUploadTransferProof { get; set; }
    public string? TransferProofUrl { get; set; }

    /// <summary>O'tkazma uchun karta — faqat Transfer usulida va yakuniy summa ma'lum bo'lgach (Ready'dan) ko'rsatiladi.</summary>
    public string? TransferCardNumber { get; set; }
    public string? TransferCardHolder { get; set; }
}
