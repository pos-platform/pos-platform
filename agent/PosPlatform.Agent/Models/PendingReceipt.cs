namespace PosPlatform.Agent.Models;

/// <summary>Backenddan keladigan kutayotgan chek (DTO bilan mos).</summary>
public class PendingReceipt
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? BranchId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public List<PendingReceiptItem> Items { get; set; } = new();
}

public class PendingReceiptItem
{
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    /// <summary>Markirovka (Asl Belgisi) kodi — fiskal chekning Labels maydoniga uzatiladi (ixtiyoriy).</summary>
    public string? MarkingCode { get; set; }
    public string Unit { get; set; } = "Piece";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
}
