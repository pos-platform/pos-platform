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

    /// <summary>Shtrix-kod (ixtiyoriy).</summary>
    public string? Barcode { get; set; }

    public decimal Price { get; set; }

    public Unit Unit { get; set; } = Unit.Piece;

    /// <summary>Tarozida tortib sotiladigan mahsulotmi.</summary>
    public bool IsWeighed { get; set; }

    /// <summary>Tarozi uchun PLU raqami (ixtiyoriy).</summary>
    public int? PluNumber { get; set; }

    /// <summary>Idish/qadoq vazni — tortishda ayriladi (ixtiyoriy).</summary>
    public decimal? Tare { get; set; }

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }
}
