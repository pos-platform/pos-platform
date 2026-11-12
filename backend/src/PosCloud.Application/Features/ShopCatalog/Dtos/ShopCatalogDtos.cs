using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopCatalog.Dtos;

/// <summary>Ilovadagi do'kon = tashkilotning bitta filiali.</summary>
public class ShopStoreDto
{
    /// <summary>Filial ID. Katalog so'rovlarida shu ishlatiladi.</summary>
    public Guid Id { get; set; }

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
}

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
}

public class ShopProductDetailDto : ShopProductDto
{
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? CategoryName { get; set; }
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

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
