using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopCatalog.Dtos;

/// <summary>Ilovadagi do'kon = tashkilotning bitta filiali.</summary>
public class ShopStoreDto
{
    /// <summary>Filial ID. Katalog so'rovlarida shu ishlatiladi.</summary>
    public Guid Id { get; set; }

    /// <summary>Tashkilot ID (bonus/taklif so'rovlari uchun: /api/me/referral?organizationId=).</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Do'kon nomi (online sozlamadagi nom, bo'lmasa tashkilot nomi).</summary>
    public string Name { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Mijoz koordinatasi berilgan bo'lsa — masofa (km).</summary>
    public double? DistanceKm { get; set; }

    public bool IsFeatured { get; set; }

    /// <summary>Ish vaqti ichida va buyurtma qabul qilish yoqilgan.</summary>
    public bool IsOpenNow { get; set; }
    public bool IsAcceptingOrders { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }

    public bool DeliveryEnabled { get; set; }
    public bool PickupEnabled { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal? FreeDeliveryFrom { get; set; }

    /// <summary>Taxminiy yetkazish vaqti (daqiqa). Null — do'kon kiritmagan.</summary>
    public int? DeliveryMinMinutes { get; set; }
    public int? DeliveryMaxMinutes { get; set; }

    public bool AllowCash { get; set; }
    public bool AllowTransfer { get; set; }
    public bool AllowCardOnDelivery { get; set; }

    /// <summary>Online to'lov (yig'ilgandan keyin): Payme / Click.</summary>
    public bool AllowPayme { get; set; }
    public bool AllowClick { get; set; }

    /// <summary>Bonus bilan to'lash va mahsulotlar summasidan maksimal foiz.</summary>
    public bool BonusPayEnabled { get; set; }
    public int BonusMaxPercent { get; set; }

    /// <summary>Yetkazish radiusi (km). Null — cheklovsiz.</summary>
    public double? DeliveryRadiusKm { get; set; }

    /// <summary>Mijozlar uchun yordam: telefon va Telegram username (@ siz).</summary>
    public string? SupportPhone { get; set; }
    public string? SupportTelegram { get; set; }
}

/// <summary>Alohida (white-label) do'kon ilovasi ochilganda oladigan ma'lumot: brend va ko'rinadigan filiallar.</summary>
public class ShopStoreAppDto
{
    public Guid OrganizationId { get; set; }
    public string Slug { get; set; } = string.Empty;

    /// <summary>AppName; bo'sh bo'lsa DisplayName yoki tashkilot nomi.</summary>
    public string AppName { get; set; } = string.Empty;
    public string? BrandColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public IReadOnlyList<ShopStoreDto> Branches { get; set; } = Array.Empty<ShopStoreDto>();
}

/// <summary>Do'kon ilovasining bitta platforma uchun versiya qiymatlari (null — global qiymat ishlatiladi).</summary>
public sealed record StoreAppPlatformInfo(int? MinBuild, int? LatestBuild, string? LatestVersion, string? StoreUrl);

public class ShopCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Hozir sotuvda bor mahsulotlar soni.</summary>
    public int ProductCount { get; set; }
}

public class ShopProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }

    /// <summary>Nisbiy bo'lishi mumkin ("/uploads/..") — ilova API manzilini oldiga qo'shadi.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Aksiya qo'llangan yakuniy narx.</summary>
    public decimal Price { get; set; }

    /// <summary>Aksiya bo'lsa — chegirmasiz narx (chizilgan), aks holda null.</summary>
    public decimal? OldPrice { get; set; }
    public decimal? DiscountPercent { get; set; }

    /// <summary>Sotish mumkin bo'lgan qoldiq (ombor − band qilingan).</summary>
    public decimal Available { get; set; }
    public bool InStock { get; set; }

    /// <summary>Faqat top javobida: o'rin (1..10).</summary>
    public int? Rank { get; set; }

    /// <summary>Qo'llangan aksiyaning tugash vaqti (UTC) — ortga sanash uchun.</summary>
    public DateTime? DiscountEndsAt { get; set; }

    /// <summary>Sevimlilarda (faqat token bilan so'ralganda; aks holda null).</summary>
    public bool? IsFavorite { get; set; }
}

public class ShopProductDetailDto : ShopProductDto
{
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? CategoryName { get; set; }

    /// <summary>"Kelganda xabar berish" obunasi bor (faqat token bilan; aks holda null).</summary>
    public bool? IsSubscribed { get; set; }
}

/// <summary>Bosh sahifa banneri.</summary>
public class ShopBannerDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public ShopLinkType LinkType { get; set; }
    public string? LinkValue { get; set; }
    public DateTime? EndsAt { get; set; }
}

/// <summary>Mahsulotlar kolleksiyasi.</summary>
public class ShopCollectionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? EndsAt { get; set; }
    public List<ShopProductDto> Products { get; set; } = [];
}

public class ShopStoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public List<ShopStorySlideDto> Slides { get; set; } = [];
}

public class ShopStorySlideDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public ShopLinkType LinkType { get; set; }
    public string? LinkValue { get; set; }
    public int DurationSec { get; set; }
}

/// <summary>GET /api/shop/stores/{branchId}/home javobi.</summary>
public class ShopHomeDto
{
    public List<ShopBannerDto> Banners { get; set; } = [];
    public List<ShopProductDto> TopProducts { get; set; } = [];
    public List<ShopCollectionDto> Collections { get; set; } = [];
    public int DiscountedCount { get; set; }

    /// <summary>"Yana olasizmi?" — faqat token bilan.</summary>
    public List<ShopProductDto> BuyAgain { get; set; } = [];
    public List<ShopStoryDto> Stories { get; set; } = [];
}

/// <summary>Qidiruv taklifi uchun qisqa mahsulot.</summary>
public class ShopProductBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class ShopCategoryBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ShopSearchSuggestDto
{
    public List<ShopProductBriefDto> Products { get; set; } = [];
    public List<ShopCategoryBriefDto> Categories { get; set; } = [];
}

public class CartSuggestionsRequest
{
    public List<Guid> ProductIds { get; set; } = [];
}

public class ShopProductQuery
{
    /// <summary>Nom bo'yicha qidiruv yoki aniq shtrix-kod.</summary>
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>popular (standart) | name | price_asc | price_desc</summary>
    public string? Sort { get; set; }

    /// <summary>true (standart) — faqat qoldig'i bor mahsulotlar.</summary>
    public bool InStockOnly { get; set; } = true;

    /// <summary>true — faqat aksiyadagi mahsulotlar (aksiya sahifasi).</summary>
    public bool Discounted { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
