using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mahsulot turi. Restoran rejimida Dish (ovqat) sotuvda ombordan ayirilmaydi;
    /// Good (tovar/xom-ashyo) faqat KitchenIssue orqali kamayadi. Do'kon rejimida hamma Good.
    /// </summary>
    public ProductType ProductType { get; set; } = ProductType.Good;

    /// <summary>Tayyorlash vaqti (daqiqa) — ovqat uchun ixtiyoriy.</summary>
    public int? PrepMinutes { get; set; }

    /// <summary>Ovqat retsepti (DishIngredient) — ProductType=Dish bo'lganda tovar tarkibi.</summary>
    public ICollection<DishIngredient> Ingredients { get; set; } = new List<DishIngredient>();

    /// <summary>
    /// MXIK kodi — O'zbekiston soliq tizimi (FDS) uchun majburiy
    /// mahsulot/xizmat identifikatsiya kodi.
    /// </summary>
    public string MxikCode { get; set; } = string.Empty;

    /// <summary>
    /// Avtomatik topilgan TAXMINIY MXIK — faqat taklif, chekka yozilmaydi (chekka faqat MxikCode).
    /// Odam formada tekshirib MxikCode'ga qo'llaydi; to'g'ri MxikCode saqlansa tozalanadi.
    /// </summary>
    public string? MxikSuggestion { get; set; }

    /// <summary>Taklif qayerdan olingan (masalan "tasnif", "brend").</summary>
    public string? MxikSuggestionSource { get; set; }

    /// <summary>Shtrix-kod / kod tovara (ixtiyoriy).</summary>
    public string? Barcode { get; set; }

    /// <summary>Tavsif (ixtiyoriy).</summary>
    public string? Description { get; set; }

    /// <summary>QQS (НДС) bilanmi.</summary>
    public bool IsVat { get; set; } = true;

    /// <summary>Markirovka (маркировка) qilinadimi.</summary>
    public bool IsMarked { get; set; }

    /// <summary>Markirovka kodi (Код маркировки) — markirovkali tovar uchun KMK/DataMatrix kodi (ixtiyoriy).</summary>
    public string? MarkingCode { get; set; }

    /// <summary>Qadoqdagi kod (Код на упаковке) — agregatsiyalangan qadoq kodi (ixtiyoriy).</summary>
    public string? PackageCode { get; set; }

    /// <summary>
    /// Komitent STIR (9 xona) yoki JSHSHIR (14 xona) — tovar komissiya asosida sotilsa (ixtiyoriy).
    /// Fiskal chekda tovar ostida chiqadi va OFD ga CommissionInfo sifatida uzatiladi (VMQ-943, 14-band).
    /// </summary>
    public string? CommitentTin { get; set; }

    /// <summary>Yetkazib beruvchi (ixtiyoriy).</summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Olingan narx (tannarx).</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Sotish narxi.</summary>
    public decimal Price { get; set; }

    /// <summary>Mahsulot rasmi manzili (ixtiyoriy).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Mobil ilovada ko'rinsinmi.</summary>
    public bool ShowOnline { get; set; } = true;

    /// <summary>Kassada ro'yxat boshiga qadab qo'yilganmi.</summary>
    public bool IsPinned { get; set; }

    public Unit Unit { get; set; } = Unit.Piece;

    /// <summary>Tarozida tortib sotiladigan mahsulotmi.</summary>
    public bool IsWeighed { get; set; }

    /// <summary>Tarozi uchun PLU raqami (ixtiyoriy).</summary>
    public int? PluNumber { get; set; }

    /// <summary>Idish/qadoq vazni — tortishda ayriladi (ixtiyoriy).</summary>
    public decimal? Tare { get; set; }

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    /// <summary>Mahsulot tegishli do'kon (ixtiyoriy). null = barcha do'konlar uchun umumiy.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    /// <summary>Filiallar bo'yicha qoldiqlar (BranchStock).</summary>
    public ICollection<BranchStock> Stocks { get; set; } = new List<BranchStock>();
}
