using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Hisobdan chiqarish (списание) — yaroqsiz/buzilgan/yo'qolgan tovarni ombordan
/// chiqarish. Qoldiq kamayadi.
/// </summary>
public class WriteOff : BaseEntity
{
    public string Number { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Sabab (masalan: muddati o'tgan, buzilgan).</summary>
    public string? Reason { get; set; }

    /// <summary>Chiqarilgan tovarlarning umumiy tannarxi.</summary>
    public decimal TotalCost { get; set; }

    public ICollection<WriteOffItem> Items { get; set; } = new List<WriteOffItem>();
}
