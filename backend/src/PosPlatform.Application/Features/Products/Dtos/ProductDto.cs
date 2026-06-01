using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Products.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>Ustama foiz = (Price - CostPrice) / CostPrice * 100.</summary>
    public decimal MarkupPercent { get; set; }
    public string? ImageUrl { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }
    public int? PluNumber { get; set; }
    public decimal? Tare { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static ProductDto FromEntity(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        MxikCode = p.MxikCode,
        Barcode = p.Barcode,
        CostPrice = p.CostPrice,
        Price = p.Price,
        Quantity = p.Quantity,
        MarkupPercent = p.CostPrice > 0
            ? Math.Round((p.Price - p.CostPrice) / p.CostPrice * 100m, 2)
            : 0m,
        ImageUrl = p.ImageUrl,
        Unit = p.Unit,
        IsWeighed = p.IsWeighed,
        PluNumber = p.PluNumber,
        Tare = p.Tare,
        CategoryId = p.CategoryId,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
