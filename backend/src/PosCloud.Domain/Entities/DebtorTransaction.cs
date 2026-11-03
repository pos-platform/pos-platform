using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Qarzdor bo'yicha harakat: qarz qo'shildi yoki to'lov qilindi.</summary>
public class DebtorTransaction : BaseEntity
{
    public Guid DebtorId { get; set; }
    public Debtor? Debtor { get; set; }

    public DebtTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Shu harakatdan keyingi balans (tarix uchun).</summary>
    public decimal BalanceAfter { get; set; }

    public string? Comment { get; set; }
}
