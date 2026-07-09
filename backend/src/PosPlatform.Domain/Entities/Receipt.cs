using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Savdoning fiskal cheki (1:1). Fiskal maydonlar lokal .NET agent (FDS / Rahmat MK)
/// javobidan to'ldiriladi; chek ESC/POS printerga shu ma'lumot bilan chiqariladi.
/// </summary>
public class Receipt : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public FiscalStatus FiscalStatus { get; set; } = FiscalStatus.NotSent;

    /// <summary>Soliq qaytargan fiskal belgi (FP_SIGN).</summary>
    public string? FiscalSign { get; set; }

    /// <summary>Fiskal modul identifikatori (terminal ID).</summary>
    public string? FiscalModuleId { get; set; }

    /// <summary>Kassa apparatining soliq tizimidagi ro'yxatga olish raqami (RS).</summary>
    public string? FiscalRegistrationNumber { get; set; }

    /// <summary>Soliq tizimidagi chek raqami.</summary>
    public string? FiscalReceiptNumber { get; set; }

    /// <summary>Chekdagi QR kod manzili (ofd.uz tekshiruv havolasi).</summary>
    public string? QrCodeUrl { get; set; }

    /// <summary>Fiskalizatsiya vaqti.</summary>
    public DateTime? FiscalizedAt { get; set; }

    /// <summary>So'nggi fiskal xatolik matni (agar bo'lsa).</summary>
    public string? ErrorMessage { get; set; }
}
