using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Note { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
