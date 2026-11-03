using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

public class WriteOffItem : BaseEntity
{
    public Guid WriteOffId { get; set; }
    public WriteOff? WriteOff { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public decimal CostPrice { get; set; }

    public decimal LineTotal { get; set; }
}
