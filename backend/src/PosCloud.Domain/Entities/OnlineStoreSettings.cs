using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Tashkilotning mobil ilovadagi sozlamalari (har tashkilotga bitta).
/// Yuqori qism — SuperAdmin boshqaradi, pastki qism — do'kon admini.
/// </summary>
public class OnlineStoreSettings : BaseEntity
{
    // ---- SuperAdmin ----

    /// <summary>Do'kon ilovaga chiqish so'rovini yuborgan.</summary>
    public bool IsRequested { get; set; }

    /// <summary>SuperAdmin tasdiqlagan va ilovada ko'rinadi.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Bloklangan (shikoyat, to'lov qilinmagan). Kassa ishlashda davom etadi.</summary>
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    /// <summary>Bosh sahifada yuqorida.</summary>
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Online tarif muddati. Null — cheklovsiz.</summary>
    public DateTime? PaidUntil { get; set; }

    public bool AllowCash { get; set; } = true;
    public bool AllowTransfer { get; set; } = true;
    public bool AllowCardOnDelivery { get; set; }

    // ---- Do'kon admini ----

    /// <summary>Hozir buyurtma qabul qilinadimi (band bo'lsa vaqtincha yopiladi).</summary>
    public bool IsAcceptingOrders { get; set; } = true;

    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }

    public bool DeliveryEnabled { get; set; } = true;
    public bool PickupEnabled { get; set; } = true;

    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryFee { get; set; }

    /// <summary>Shu summadan yuqori bo'lsa yetkazish bepul. Null — har doim pullik.</summary>
    public decimal? FreeDeliveryFrom { get; set; }

    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }

    /// <summary>O'tkazma uchun karta (yoki hisob) raqami va egasi — mijozga "Tayyor"dan keyin ko'rsatiladi.</summary>
    public string? TransferCardNumber { get; set; }
    public string? TransferCardHolder { get; set; }

    /// <summary>True — o'tkazma kuryerga berishdan oldin tasdiqlanishi shart. False — eshik oldida ham mumkin.</summary>
    public bool RequireTransferBeforeDispatch { get; set; } = true;
}
