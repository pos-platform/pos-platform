using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.KitchenIssues.Dtos;

public class KitchenIssueDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public Guid? ChefUserId { get; set; }
    public string? ChefName { get; set; }
    public string? Comment { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<KitchenIssueItemDto> Items { get; set; } = new();

    public static KitchenIssueDto FromEntity(KitchenIssue k) => new()
    {
        Id = k.Id,
        Number = k.Number,
        BranchId = k.BranchId,
        ChefUserId = k.ChefUserId,
        ChefName = k.ChefUser?.Username,
        Comment = k.Comment,
        TotalCost = k.TotalCost,
        CreatedAt = k.CreatedAt,
        Items = k.Items.Select(KitchenIssueItemDto.FromEntity).ToList()
    };
}

public class KitchenIssueItemDto
{
    public Guid? DishProductId { get; set; }
    public string? DishName { get; set; }
    public Guid IngredientProductId { get; set; }
    public string? IngredientName { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal LineTotal { get; set; }

    public static KitchenIssueItemDto FromEntity(KitchenIssueItem i) => new()
    {
        DishProductId = i.DishProductId,
        DishName = i.DishProduct?.Name,
        IngredientProductId = i.IngredientProductId,
        IngredientName = i.IngredientProduct?.Name,
        Quantity = i.Quantity,
        CostPrice = i.CostPrice,
        LineTotal = i.LineTotal
    };
}

public class CreateKitchenIssueRequest
{
    public Guid? BranchId { get; set; }
    public Guid? ChefUserId { get; set; }
    public string? Comment { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateKitchenIssueItemRequest> Items { get; set; } = new();
}

public class CreateKitchenIssueItemRequest
{
    /// <summary>Qaysi ovqat uchun (ixtiyoriy).</summary>
    public Guid? DishProductId { get; set; }

    [Required]
    public Guid IngredientProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }
}

/// <summary>Retsept asosida taklif qilingan tovarlar (oshpazga berishda oldindan to'ldirish uchun).</summary>
public class SuggestedIssueItemDto
{
    public Guid IngredientProductId { get; set; }
    public string? IngredientName { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>Oshxona sarfi hisoboti qatori — ovqat bo'yicha sarflangan tovarlar.</summary>
public class KitchenConsumptionRow
{
    public Guid? DishProductId { get; set; }
    public string? DishName { get; set; }
    public Guid IngredientProductId { get; set; }
    public string? IngredientName { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}
