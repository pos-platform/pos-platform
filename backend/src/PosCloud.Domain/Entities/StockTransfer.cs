using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Filiallararo tovar ko'chirish (transfer).</summary>
public class StockTransfer : BaseEntity
{
    public string Number { get; set; } = string.Empty;

    public Guid FromBranchId { get; set; }
    public Branch? FromBranch { get; set; }

    public Guid ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    public string? Comment { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    /// <summary>Ikki marta saqlanishdan himoya: forma ochilganda yaratiladigan kalit.</summary>
    public string? ClientRequestId { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
