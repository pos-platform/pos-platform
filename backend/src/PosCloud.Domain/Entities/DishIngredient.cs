using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Ovqat retsepti qatori (texnologik karta) — bitta ovqatga (DishProduct) qaysi
/// tovar (IngredientProduct) qancha ketishini bildiradi. "1 ta ovqatga umumiy tovarlar".
/// Bu ombordan oshpazga berishda (KitchenIssue) tovarlarni oldindan to'ldirish va
/// hisobot uchun ishlatiladi.
/// </summary>
public class DishIngredient : BaseEntity
{
    /// <summary>Ovqat (ProductType=Dish).</summary>
    public Guid DishProductId { get; set; }
    public Product? DishProduct { get; set; }

    /// <summary>Tarkibiy tovar (ProductType=Good).</summary>
    public Guid IngredientProductId { get; set; }
    public Product? IngredientProduct { get; set; }

    /// <summary>Bitta ovqatga ketadigan tovar miqdori.</summary>
    public decimal Quantity { get; set; }
}
