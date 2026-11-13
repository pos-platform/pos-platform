using System.ComponentModel.DataAnnotations;

namespace PosCloud.Application.Features.OnlineStores.Dtos;

/// <summary>SuperAdmin ro'yxati uchun qator.</summary>
public class OnlineStoreListItemDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool OrganizationActive { get; set; }
    public int StoreBranchCount { get; set; }
    public bool HasSettings { get; set; }
    public bool IsRequested { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PaidUntil { get; set; }
    public bool IsAcceptingOrders { get; set; }

    /// <summary>White-label ilova slug'i va nomi (sozlanmagan bo'lsa null).</summary>
    public string? Slug { get; set; }
    public string? AppName { get; set; }

    /// <summary>Oxirgi 30 kundagi online buyurtmalar soni.</summary>
    public int OrdersLast30Days { get; set; }
}

/// <summary>Sozlamalarning to'liq ko'rinishi (SuperAdmin va do'kon admini uchun bir xil).</summary>
public class OnlineStoreSettingsDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;

    // SuperAdmin qismi (do'kon admini uchun faqat o'qish)
    public bool IsRequested { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PaidUntil { get; set; }
    public bool AllowCash { get; set; }
    public bool AllowTransfer { get; set; }
    public bool AllowCardOnDelivery { get; set; }
    public bool AllowPayme { get; set; }
    public bool AllowClick { get; set; }

    // Do'kon admini qismi
    public bool IsAcceptingOrders { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public bool DeliveryEnabled { get; set; }
    public bool PickupEnabled { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal? FreeDeliveryFrom { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public string? TransferCardNumber { get; set; }
    public string? TransferCardHolder { get; set; }
    public bool RequireTransferBeforeDispatch { get; set; }
    public int? DeliveryMinMinutes { get; set; }
    public int? DeliveryMaxMinutes { get; set; }

    /// <summary>"Yangi" buyurtma shuncha daqiqada qabul qilinmasa avtomatik bekor qilinadi. 0 — o'chiq.</summary>
    public int AutoCancelMinutes { get; set; }

    /// <summary>Yetkazish radiusi (km). Null — cheklovsiz.</summary>
    public double? DeliveryRadiusKm { get; set; }
    public string? DeliveryServiceMxikCode { get; set; }
    public bool BonusPayEnabled { get; set; }
    public int BonusMaxPercent { get; set; }

    // White-label mobil ilova (SuperAdmin)
    public string? Slug { get; set; }
    public string? AppName { get; set; }
    public string? AppPackageId { get; set; }
    public string? BrandColor { get; set; }
    public int? AndroidMinBuild { get; set; }
    public int? AndroidLatestBuild { get; set; }
    public string? AndroidLatestVersion { get; set; }
    public string? AndroidStoreUrl { get; set; }
    public int? IosMinBuild { get; set; }
    public int? IosLatestBuild { get; set; }
    public string? IosLatestVersion { get; set; }
    public string? IosStoreUrl { get; set; }

    /// <summary>Ilovada hozir ko'rinadimi (yoqilgan, bloklanmagan, tarif amal qiladi, tashkilot faol).</summary>
    public bool IsVisible { get; set; }

    // Bosqich 9: yordam va do'stni taklif
    public string? SupportPhone { get; set; }
    public string? SupportTelegram { get; set; }
    public bool ReferralEnabled { get; set; }
    public decimal ReferrerBonus { get; set; }
    public decimal RefereeBonus { get; set; }
}

/// <summary>SuperAdmin o'zgartiradigan qism.</summary>
public class UpdateOnlineStoreAdminRequest
{
    public bool IsEnabled { get; set; }
    public bool IsBlocked { get; set; }

    [MaxLength(500)]
    public string? BlockReason { get; set; }

    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Null — cheklovsiz.</summary>
    public DateTime? PaidUntil { get; set; }

    public bool AllowCash { get; set; } = true;
    public bool AllowTransfer { get; set; } = true;
    public bool AllowCardOnDelivery { get; set; }

    /// <summary>Online to'lov (yig'ilgandan keyin): Payme / Click.</summary>
    public bool AllowPayme { get; set; }
    public bool AllowClick { get; set; }
}

/// <summary>SuperAdmin: alohida (white-label) mobil ilova sozlamalari. Hammasi ixtiyoriy; bo'sh — tozalash.</summary>
public class UpdateStoreAppRequest
{
    /// <summary>^[a-z0-9-]{3,40}$, unik.</summary>
    public string? Slug { get; set; }

    public string? AppName { get; set; }

    /// <summary>^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$, unik.</summary>
    public string? AppPackageId { get; set; }

    /// <summary>#RRGGBB.</summary>
    public string? BrandColor { get; set; }

    public int? AndroidMinBuild { get; set; }
    public int? AndroidLatestBuild { get; set; }
    public string? AndroidLatestVersion { get; set; }
    public string? AndroidStoreUrl { get; set; }

    public int? IosMinBuild { get; set; }
    public int? IosLatestBuild { get; set; }
    public string? IosLatestVersion { get; set; }
    public string? IosStoreUrl { get; set; }
}

/// <summary>Do'kon admini o'zgartiradigan qism.</summary>
public class UpdateOnlineStoreSettingsRequest
{
    public bool IsAcceptingOrders { get; set; } = true;

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool DeliveryEnabled { get; set; } = true;
    public bool PickupEnabled { get; set; } = true;

    [Range(0, 1_000_000_000)]
    public decimal MinOrderAmount { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal DeliveryFee { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal? FreeDeliveryFrom { get; set; }

    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }

    [MaxLength(32)]
    public string? TransferCardNumber { get; set; }

    [MaxLength(200)]
    public string? TransferCardHolder { get; set; }

    public bool RequireTransferBeforeDispatch { get; set; } = true;

    /// <summary>Taxminiy yetkazish vaqti (5..1440 daqiqa). Ikkalasi birga yoki ikkalasi null.</summary>
    [Range(5, 1440)]
    public int? DeliveryMinMinutes { get; set; }

    [Range(5, 1440)]
    public int? DeliveryMaxMinutes { get; set; }

    /// <summary>0 — avtomatik bekor qilish o'chiq.</summary>
    [Range(0, 1440)]
    public int AutoCancelMinutes { get; set; } = 30;

    /// <summary>Yetkazish radiusi (filialdan, km). Null — cheklovsiz.</summary>
    [Range(0.1, 500)]
    public double? DeliveryRadiusKm { get; set; }

    /// <summary>"Yetkazib berish xizmati" MXIK kodi — kiritilsa yetkazish narxi fiskal chekka alohida qator bo'lib tushadi.</summary>
    [MaxLength(20)]
    public string? DeliveryServiceMxikCode { get; set; }

    /// <summary>Bonus bilan to'lash va mahsulotlar summasidan maksimal foiz (0..100).</summary>
    public bool BonusPayEnabled { get; set; }

    [Range(0, 100)]
    public int BonusMaxPercent { get; set; }

    /// <summary>Mijozlar uchun yordam telefoni.</summary>
    [MaxLength(20)]
    public string? SupportPhone { get; set; }

    /// <summary>Telegram username (@ siz; @ bilan kelsa olib tashlanadi).</summary>
    [MaxLength(33)]
    public string? SupportTelegram { get; set; }

    /// <summary>Do'stni taklif qilish dasturi va bonuslari (ball).</summary>
    public bool ReferralEnabled { get; set; }

    [Range(0, 1_000_000)]
    public decimal ReferrerBonus { get; set; }

    [Range(0, 1_000_000)]
    public decimal RefereeBonus { get; set; }
}

public class SetAcceptingOrdersRequest
{
    public bool IsAccepting { get; set; }
}
