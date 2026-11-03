using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Yetkazib beruvchiga to'lov (Платёж). To'lov yetkazib beruvchi balansini (qarzni) kamaytiradi.
/// </summary>
public class SupplierPayment : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal Amount { get; set; }

    public string? Comment { get; set; }
}
