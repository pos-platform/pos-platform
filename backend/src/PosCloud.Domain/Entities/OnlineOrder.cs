using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Mobil ilovadan kelgan buyurtma. "Ready" holatida oddiy <see cref="Sale"/> yaratiladi
/// (fiskal chek, ombordan ayirish, hisobotlar o'sha orqali). OrganizationId qo'lda to'ldiriladi,
/// chunki mijoz so'rovida joriy tashkilot yo'q.
/// </summary>
public class OnlineOrder : BaseEntity
{
    /// <summary>Tashkilot ichida ketma-ket raqam (#1045).</summary>
    public int Number { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    /// <summary>Do'kondagi mijoz yozuvi (bonus uchun).</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Xodim ko'rishi uchun nusxa (ShopUser global va tenant filtri ostida ko'rinmaydi).
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public OnlineOrderStatus Status { get; set; } = OnlineOrderStatus.New;
    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Delivery;

    /// <summary>Cash, Card (kuryer terminali yoki kassa) yoki Transfer (kartaga o'tkazma).</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.Unpaid;

    // Manzil nusxasi (mijoz keyin manzilini o'zgartirsa ham buyurtmada saqlanadi).
    public string? AddressLine { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Null — "imkon qadar tez".</summary>
    public DateTime? ScheduledFor { get; set; }

    public bool Contactless { get; set; }
    public string? Comment { get; set; }
    public OutOfStockPolicy OutOfStockPolicy { get; set; } = OutOfStockPolicy.CallMe;

    public decimal ItemsTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Mijoz yuklagan o'tkazma chekining rasmi.</summary>
    public string? TransferProofUrl { get; set; }
    public Guid? PaymentConfirmedByUserId { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }

    /// <summary>"Ready"da yaratilgan savdo.</summary>
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<OnlineOrderItem> Items { get; set; } = new List<OnlineOrderItem>();
}

/// <summary>Online buyurtma qatori. Narx buyurtma paytidagi nusxa.</summary>
public class OnlineOrderItem : BaseEntity
{
    public Guid OnlineOrderId { get; set; }
    public OnlineOrder? OnlineOrder { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    /// <summary>Mijoz so'ragan miqdor.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Yig'ishda haqiqatda olingan miqdor (tarozi, tugagan). Null — hali yig'ilmagan.</summary>
    public decimal? PickedQuantity { get; set; }

    public decimal LineTotal { get; set; }

    /// <summary>Yig'ishda olib tashlangan (tugagan).</summary>
    public bool IsRemoved { get; set; }

    /// <summary>Almashtirilgan bo'lsa — asl qator (yangi qator shunga ishora qiladi).</summary>
    public Guid? ReplacesItemId { get; set; }
}
