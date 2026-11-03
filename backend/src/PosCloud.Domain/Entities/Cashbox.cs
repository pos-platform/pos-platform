using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>Kassa (Касса) — do'kondagi sotuv nuqtasi. Desktop POS shu kassada ishlaydi.</summary>
public class Cashbox : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Kassadagi joriy naqd (smena/sotuvlardan yangilanadi).</summary>
    public decimal Balance { get; set; }
}
