using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Ombordan oshpazga berish qatori — qaysi ovqat uchun (DishProduct) qaysi tovar
/// (IngredientProduct) qancha berilgani. Tovar ombordan ayiriladi.
/// </summary>
public class KitchenIssueItem : BaseEntity
{
    public Guid KitchenIssueId { get; set; }
    public KitchenIssue? KitchenIssue { get; set; }

    /// <summary>Qaysi ovqat uchun berilgani (ixtiyoriy — umumiy sarf bo'lishi mumkin).</summary>
    public Guid? DishProductId { get; set; }
    public Product? DishProduct { get; set; }

    /// <summary>Berilgan tovar (ProductType=Good).</summary>
    public Guid IngredientProductId { get; set; }
    public Product? IngredientProduct { get; set; }

    public decimal Quantity { get; set; }

    public decimal CostPrice { get; set; }

    public decimal LineTotal { get; set; }
}
