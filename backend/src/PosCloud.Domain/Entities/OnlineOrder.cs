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

    /// <summary>Yig'ish boshlangan vaqt.</summary>
    public DateTime? PickingStartedAt { get; set; }

    /// <summary>Kuryerga berishda kiritiladi (mijozga ko'rsatiladi).</summary>
    public string? CourierName { get; set; }
    public string? CourierPhone { get; set; }

    /// <summary>Ilova yuborgan idempotentlik kaliti: tarmoq uzilib qayta yuborilsa ikkinchi buyurtma yaratilmaydi.</summary>
    public string? ClientRequestId { get; set; }

    /// <summary>Biriktirilgan kuryer (xodim, Role = Courier). Null — kuryer ismi qo'lda kiritilgan yoki hali berilmagan.</summary>
    public Guid? CourierUserId { get; set; }
    public User? CourierUser { get; set; }

    /// <summary>Kuryer mijozdan olgan naqd summa (yetkazilganda).</summary>
    public decimal? CourierCashCollected { get; set; }

    /// <summary>Kuryer naqdni kassaga topshirgan vaqt (kirim yozilgan).</summary>
    public DateTime? CourierCashHandedOverAt { get; set; }

    /// <summary>"Tayyor"da "naqd kuryerda" chiqimi yozilgan — topshirish (kirim) kutiladi.</summary>
    public bool CourierCashExpected { get; set; }

    /// <summary>Oxirgi muvaffaqiyatsiz yetkazish (kuryer tarixi uchun).</summary>
    public Guid? CourierFailedByUserId { get; set; }
    public DateTime? CourierFailedAt { get; set; }

    /// <summary>Promokod (ID va matn nusxasi) va uning chegirmasi.</summary>
    public Guid? PromoCodeId { get; set; }
    public string? PromoCode { get; set; }
    public decimal PromoDiscount { get; set; }

    /// <summary>Bonus (ball) bilan to'lanadigan qism. "Tayyor"da balldan yechiladi.</summary>
    public decimal BonusUsed { get; set; }

    /// <summary>"Tayyor"da ball yechildi (qayta yechilmasligi va bekor qilinganda qaytarilishi uchun).</summary>
    public bool BonusCharged { get; set; }

    /// <summary>Transfer usulida online to'lov tizimi (Payme/Click) tanlangan — karta raqami o'rniga to'lov tugmasi.</summary>
    public PaymentProvider? OnlinePaymentProvider { get; set; }

    /// <summary>Mijoz bahosi (1–5), izoh, teglar (vergul bilan) va vaqti — faqat Delivered'dan keyin.</summary>
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }
    public string? RatingTags { get; set; }
    public DateTime? RatedAt { get; set; }

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

    /// <summary>Markirovka (DataMatrix) kodlari — markirovkali dona tovarda soni butun miqdorga teng bo'lishi shart.</summary>
    public List<string> MarkingCodes { get; set; } = new();
}
