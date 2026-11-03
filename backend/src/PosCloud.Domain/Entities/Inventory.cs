using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

public enum InventoryStatus { Draft = 0, Completed = 1 }

/// <summary>
/// Inventarizatsiya — ombordagi haqiqiy qoldiqni tizim bilan solishtirish.
/// Draft holatida yaratiladi; Apply qilinganda product.Quantity tuzatiladi.
/// </summary>
public class Inventory : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.Draft;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}

public class InventoryItem : BaseEntity
{
    public Guid InventoryId { get; set; }
    public Inventory? Inventory { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Inventarizatsiya yaratilgan paytdagi tizim qoldig'i (snapshot).</summary>
    public decimal ExpectedQty { get; set; }

    /// <summary>Jismoniy sanaydagi haqiqiy miqdor.</summary>
    public decimal ActualQty { get; set; }
}
