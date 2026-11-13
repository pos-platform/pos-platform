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

    /// <summary>Online to'lov (yig'ilgandan keyin, yakuniy summa bo'yicha).</summary>
    public bool AllowPayme { get; set; }
    public bool AllowClick { get; set; }

    // ---- SuperAdmin: alohida (white-label) mobil ilova ----

    /// <summary>Ilova identifikatori (STORE_SLUG), unik, ^[a-z0-9-]{3,40}$.</summary>
    public string? Slug { get; set; }

    /// <summary>Ilova nomi (≤30). Bo'sh bo'lsa — DisplayName yoki tashkilot nomi.</summary>
    public string? AppName { get; set; }

    /// <summary>Android applicationId / iOS bundle id, unik.</summary>
    public string? AppPackageId { get; set; }

    /// <summary>Brend rangi (#RRGGBB).</summary>
    public string? BrandColor { get; set; }

    public int? AndroidMinBuild { get; set; }
    public int? AndroidLatestBuild { get; set; }
    public string? AndroidLatestVersion { get; set; }
    public string? AndroidStoreUrl { get; set; }

    public int? IosMinBuild { get; set; }
    public int? IosLatestBuild { get; set; }
    public string? IosLatestVersion { get; set; }
    public string? IosStoreUrl { get; set; }

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

    /// <summary>Taxminiy yetkazish vaqti oralig'i (daqiqa). Ikkalasi birga yoki ikkalasi null.</summary>
    public int? DeliveryMinMinutes { get; set; }
    public int? DeliveryMaxMinutes { get; set; }

    /// <summary>"Yangi" buyurtma shuncha daqiqada qabul qilinmasa avtomatik bekor qilinadi. 0 — o'chiq.</summary>
    public int AutoCancelMinutes { get; set; } = 30;

    /// <summary>Yetkazish radiusi (filialdan, km). Null — cheklovsiz.</summary>
    public double? DeliveryRadiusKm { get; set; }

    /// <summary>"Yetkazib berish xizmati" MXIK kodi. Kiritilsa yetkazish narxi fiskal chekka alohida qator bo'lib tushadi.</summary>
    public string? DeliveryServiceMxikCode { get; set; }

    /// <summary>Bonus (ball) bilan to'lash yoqilganmi va mahsulotlar summasining ko'pi bilan necha foizi.</summary>
    public bool BonusPayEnabled { get; set; }
    public int BonusMaxPercent { get; set; }

    /// <summary>Mijozlar uchun yordam: telefon va Telegram username (@ siz).</summary>
    public string? SupportPhone { get; set; }
    public string? SupportTelegram { get; set; }

    /// <summary>Do'stni taklif qilish dasturi va bonuslari (ball).</summary>
    public bool ReferralEnabled { get; set; }
    public decimal ReferrerBonus { get; set; }
    public decimal RefereeBonus { get; set; }
}
