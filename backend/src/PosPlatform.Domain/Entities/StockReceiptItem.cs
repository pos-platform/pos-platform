using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

public class StockReceiptItem : BaseEntity
{
    public Guid StockReceiptId { get; set; }
    public StockReceipt? StockReceipt { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Kirim tannarxi (birlik uchun).</summary>
    public decimal CostPrice { get; set; }

    public decimal LineTotal { get; set; }

    /// <summary>Partiya/seriya raqami (ixtiyoriy).</summary>
    public string? BatchNumber { get; set; }

    /// <summary>Yaroqlilik muddati (ixtiyoriy).</summary>
    public DateTime? ExpiryDate { get; set; }
}
