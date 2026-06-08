using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Filial bo'yicha tovar qoldig'i.
/// Har bir (BranchId, ProductId) jufti uchun bitta yozuv bo'ladi.
/// </summary>
public class BranchStock : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Shu filial omborida mavjud miqdor.</summary>
    public decimal Quantity { get; set; }
}
