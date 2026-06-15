using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Dishes.Dtos;

/// <summary>Ovqat (ProductType=Dish) + retsept tarkibi.</summary>
public class DishDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? PrepMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public List<DishIngredientDto> Ingredients { get; set; } = new();

    public static DishDto FromEntity(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        PrepMinutes = p.PrepMinutes,
        ImageUrl = p.ImageUrl,
        Ingredients = p.Ingredients.Select(DishIngredientDto.FromEntity).ToList()
    };
}

public class DishIngredientDto
{
    public Guid IngredientProductId { get; set; }
    public string? IngredientName { get; set; }
    public decimal Quantity { get; set; }

    public static DishIngredientDto FromEntity(DishIngredient i) => new()
    {
        IngredientProductId = i.IngredientProductId,
        IngredientName = i.IngredientProduct?.Name,
        Quantity = i.Quantity
    };
}

/// <summary>Ovqat retseptini to'liq almashtirish (set).</summary>
public class SetRecipeRequest
{
    [Required]
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public class RecipeItemRequest
{
    [Required]
    public Guid IngredientProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }
}
