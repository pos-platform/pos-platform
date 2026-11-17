using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Note { get; set; }

    /// <summary>Ota bo'lim (ikki darajali daraxt: bo'lim → ichki bo'lim). Null — asosiy bo'lim.</summary>
    public Guid? ParentId { get; set; }

    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>Ilovada tartib (kichigi oldin).</summary>
    public int SortOrder { get; set; }

    /// <summary>Ilovadagi bo'lim rasmi/ikonkasi.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Mijoz ilovasida ko'rinadimi (ichki/xizmat kategoriyalari va 18+ uchun false).</summary>
    public bool ShowOnline { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
