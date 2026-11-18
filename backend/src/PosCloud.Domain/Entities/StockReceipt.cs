using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Tovar kirimi (Накладная). Yetkazib beruvchidan kelgan tovarlar omborga
/// qo'shiladi, tannarx yangilanadi, yetkazib beruvchi balansiga qarz qo'shiladi.
/// </summary>
public class StockReceipt : BaseEntity
{
    public string Number { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string? Comment { get; set; }

    /// <summary>Kirim umumiy summasi (qatorlar tannarx*miqdor yig'indisi).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Holat: bekor qilingan kirim qoldiq va qarzga ta'sir qilmaydi.</summary>
    public StockDocumentStatus Status { get; set; } = StockDocumentStatus.Active;
    public DateTime? CancelledAt { get; set; }

    /// <summary>Ikki marta saqlanishdan himoya: forma ochilganda yaratiladigan kalit.</summary>
    public string? ClientRequestId { get; set; }

    public ICollection<StockReceiptItem> Items { get; set; } = new List<StockReceiptItem>();
}
