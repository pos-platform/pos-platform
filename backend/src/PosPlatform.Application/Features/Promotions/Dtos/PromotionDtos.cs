using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Promotions.Dtos;

public class PromotionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Hozir amal qilyaptimi (IsActive + sana oralig'ida).</summary>
    public bool IsCurrentlyActive { get; set; }

    public static PromotionDto FromEntity(Promotion p, DateTime nowUtc) => new()
    {
        Id = p.Id,
        Name = p.Name,
        DiscountPercent = p.DiscountPercent,
        ProductId = p.ProductId,
        ProductName = p.Product?.Name,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        IsCurrentlyActive = PromotionCalculator.IsActiveAt(p, nowUtc)
    };
}

public class CreatePromotionRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    /// <summary>Aniq mahsulotga (ixtiyoriy).</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Kategoriyaga (ixtiyoriy). ProductId bilan birga bo'lmasligi kerak.</summary>
    public Guid? CategoryId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
