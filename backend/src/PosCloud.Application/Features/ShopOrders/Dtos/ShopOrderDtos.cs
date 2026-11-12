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

    /// <summary>Promokod (ixtiyoriy). Yaroqsiz bo'lsa quote'da promoMessage, yaratishda 400.</summary>
    [MaxLength(50)]
    public string? PromoCode { get; set; }

    /// <summary>Bonus (ball) bilan to'lash — faqat kirgan mijoz va do'konda yoqilgan bo'lsa.</summary>
    public bool UseBonus { get; set; }

    /// <summary>Yetkazishda majburiy (yaratishda): mijozning saqlangan manzili. Quote'da — yetkazish hududini tekshirish uchun.</summary>
    public Guid? AddressId { get; set; }
}

public class CreateShopOrderRequest : ShopOrderQuoteRequest
{
    /// <summary>Cash, Card (kuryer terminali) yoki Transfer.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>Transfer + Payme/Click: yig'ilgandan keyin online to'lov (karta raqami ko'rsatilmaydi).</summary>
    public PaymentProvider? OnlinePaymentProvider { get; set; }

    /// <summary>Null — imkon qadar tez. UTC.</summary>
    public DateTime? ScheduledFor { get; set; }

    public bool Contactless { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public OutOfStockPolicy OutOfStockPolicy { get; set; } = OutOfStockPolicy.CallMe;

    /// <summary>Idempotentlik kaliti (ilova har yangi buyurtma uchun yaratadi). Shu kalit bilan buyurtma bo'lsa — o'sha qaytariladi.</summary>
    [MaxLength(64)]
    public string? ClientRequestId { get; set; }
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

    /// <summary>Promokod chegirmasi; yaroqsiz bo'lsa 0 va promoMessage'da sabab.</summary>
    public decimal PromoDiscount { get; set; }
    public string? PromoMessage { get; set; }

    /// <summary>Mijozning shu do'kondagi balli va shu buyurtmada ishlatiladigan qismi.</summary>
    public decimal BonusAvailable { get; set; }
    public decimal BonusUsed { get; set; }

    /// <summary>Yetkazish hududi: false — manzil radiusdan tashqarida (buyurtma berilmaydi).</summary>
    public bool DeliveryAvailable { get; set; } = true;
    public string? DeliveryMessage { get; set; }
    public double? DistanceKm { get; set; }
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
    public DateTime? PickingStartedAt { get; set; }

    public string? CourierName { get; set; }
    public string? CourierPhone { get; set; }

    /// <summary>Savdo cheki raqami ("Ready"dan keyin).</summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>Fiskal chekning QR havolasi (ofd.uz). Null — chek hali fiskallashmagan yoki nofiskal.</summary>
    public string? FiscalQrUrl { get; set; }

    /// <summary>Taxminiy yetkazish oralig'i (UTC). Faqat yetkazishda, tugamagan buyurtmada va do'kon vaqtni kiritgan bo'lsa.</summary>
    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }

    /// <summary>Mijoz hali bekor qila oladimi (Picking'gacha).</summary>
    public bool CanCancel { get; set; }

    /// <summary>O'tkazma cheki yuklash mumkinmi.</summary>
    public bool CanUploadTransferProof { get; set; }
    public string? TransferProofUrl { get; set; }

    /// <summary>O'tkazma uchun karta — faqat Transfer usulida va yakuniy summa ma'lum bo'lgach (Ready'dan) ko'rsatiladi.</summary>
    public string? TransferCardNumber { get; set; }
    public string? TransferCardHolder { get; set; }

    public string? PromoCode { get; set; }
    public decimal PromoDiscount { get; set; }
    public decimal BonusUsed { get; set; }

    /// <summary>Transfer + Payme/Click tanlangan bo'lsa — provayder.</summary>
    public PaymentProvider? OnlinePaymentProvider { get; set; }

    /// <summary>Hozir online to'lash mumkinmi (do'kon ruxsati, to'lanmagan, yig'ilgan, savdo bor).</summary>
    public bool CanPayOnline { get; set; }

    /// <summary>Mavjud provayderlar: "Payme", "Click".</summary>
    public List<string> OnlineProviders { get; set; } = [];
}

/// <summary>Online to'lov havolasi so'rovi.</summary>
public class PayShopOrderRequest
{
    public PaymentProvider Provider { get; set; }
}

public class PayShopOrderResponse
{
    public string PaymentUrl { get; set; } = string.Empty;
}

/// <summary>GET /api/me/bonus javobi.</summary>
public class ShopBonusDto
{
    public decimal Points { get; set; }
}
