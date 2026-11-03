using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Kassa harakati: naqd kirim, chiqim yoki inkassatsiya.</summary>
public class CashMovement : BaseEntity
{
    public CashMovementType Type { get; set; }

    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Qaysi smenada ro'y bergan (null = smena tashqarisida).</summary>
    public Guid? CashShiftId { get; set; }
    public CashShift? CashShift { get; set; }
}
