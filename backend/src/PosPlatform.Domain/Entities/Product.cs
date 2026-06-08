using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MXIK kodi — O'zbekiston soliq tizimi (FDS) uchun majburiy
    /// mahsulot/xizmat identifikatsiya kodi.
    /// </summary>
    public string MxikCode { get; set; } = string.Empty;

    /// <summary>Shtrix-kod / kod tovara (ixtiyoriy).</summary>
    public string? Barcode { get; set; }

    /// <summary>Tavsif (ixtiyoriy).</summary>
    public string? Description { get; set; }

    /// <summary>QQS (НДС) bilanmi.</summary>
    public bool IsVat { get; set; } = true;

    /// <summary>Markirovka (маркировка) qilinadimi.</summary>
    public bool IsMarked { get; set; }

    /// <summary>Yetkazib beruvchi (ixtiyoriy).</summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Olingan narx (tannarx).</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Sotish narxi.</summary>
    public decimal Price { get; set; }

    /// <summary>Ombordagi qoldiq (kirimda oshadi, sotuvda kamayadi).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Mahsulot rasmi manzili (ixtiyoriy).</summary>
    public string? ImageUrl { get; set; }

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
}
