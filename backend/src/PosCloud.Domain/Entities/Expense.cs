using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>Xarajat (расход) — kassadan/do'kondan naqd chiqim (ijara, oylik, va h.k.).</summary>
public class Expense : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
