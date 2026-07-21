using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Savdo qatori. Narx/nom/MXIK kabi maydonlar savdo paytidagi qiymat bilan
/// "muzlatiladi" (snapshot) — keyin mahsulot o'zgarsa ham chek va fiskal ma'lumot
/// o'zgarmaydi.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    // --- Snapshotlar ---
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public Unit Unit { get; set; }

    /// <summary>
    /// Markirovka (Asl Belgisi) kodi — GS1 DataMatrix ning to'liq satri. Faqat markirovkali
    /// tovarda to'ldiriladi; fiskal chekning <c>Labels</c> maydoniga uzatiladi va OFD orqali
    /// tovar oborotdan chiqariladi. Har bir jismoniy dona uchun noyob (shuning uchun bunday
    /// qatorda Quantity doim 1).
    /// </summary>
    public string? MarkingCode { get; set; }

    /// <summary>Miqdor (donada butun, kg'da kasrli).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Savdo paytidagi birlik narxi.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Savdo paytidagi tannarx (snapshot) — foyda hisobi uchun.</summary>
    public decimal CostPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    /// <summary>QQS stavkasi (%), fiskal uchun. O'zbekistonda odatda 12%.</summary>
    public decimal VatPercent { get; set; }

    /// <summary>Qator QQS summasi.</summary>
    public decimal VatAmount { get; set; }

    /// <summary>Qator yakuniy summasi = UnitPrice * Quantity - DiscountAmount.</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Qator izohi / oddiy modifikator (masalan "shakarsiz", "achchiq emas").</summary>
    public string? Note { get; set; }

    /// <summary>Restoran: ushbu qator oshxonaga yuborilganmi (faqat yangi qatorlar chop etiladi).</summary>
    public bool SentToKitchen { get; set; }
}
