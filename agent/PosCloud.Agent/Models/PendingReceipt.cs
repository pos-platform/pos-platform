namespace PosCloud.Agent.Models;

/// <summary>Backenddan keladigan kutayotgan chek (DTO bilan mos).</summary>
public class PendingReceipt
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>Chek sarlavhasi: tashkilot nomi, STIR va savdo manzili (VMQ-943, 1–3-bandlar).</summary>
    public string? OrganizationName { get; set; }
    public string? OrganizationStir { get; set; }
    public string? Address { get; set; }

    /// <summary>Chek turi: "Sale" — sotuv, "Refund" — qaytarish.</summary>
    public string Type { get; set; } = "Sale";

    public string PaymentMethod { get; set; } = "Cash";
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }

    /// <summary>Aralash (Mixed) to'lovda karta qismi; naqd = TotalAmount − CardAmount. Boshqalarda 0.</summary>
    public decimal CardAmount { get; set; }

    /// <summary>Qaytarish chekida — asl fiskal chek rekvizitlari. Sotuvda null.</summary>
    public RefundInfo? RefundInfo { get; set; }

    public List<PendingReceiptItem> Items { get; set; } = new();

    public bool IsRefund => Type.Equals("Refund", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Qaytarilayotgan asl fiskal chek (OFD RefundInfo).</summary>
public class RefundInfo
{
    public string TerminalId { get; set; } = string.Empty;
    public string ReceiptSeq { get; set; } = string.Empty;

    /// <summary>Asl chek fiskalizatsiya vaqti (UTC) — modulga lokal vaqtda yuboriladi.</summary>
    public DateTime DateTime { get; set; }
    public string FiscalSign { get; set; } = string.Empty;
}

public class PendingReceiptItem
{
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    /// <summary>Markirovka (Asl Belgisi) kodi — fiskal chekning Labels maydoniga uzatiladi (ixtiyoriy).</summary>
    public string? MarkingCode { get; set; }

    /// <summary>Qadoq kodi (Код упаковки, tasnif.soliq.uz) — chek qatorining PackageCode maydoni (ixtiyoriy).</summary>
    public string? PackageCode { get; set; }

    /// <summary>Komitent STIR (9) / JSHSHIR (14) — chek qatorining CommissionInfo maydoni (ixtiyoriy).</summary>
    public string? CommitentTin { get; set; }
    public string Unit { get; set; } = "Piece";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
}
