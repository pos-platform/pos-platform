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
    /// <summary>
    /// Joriy foydalanuvchi filialidagi qoldiq.
    /// Admin uchun barcha filiallar yig'indisi.
    /// </summary>
    public decimal Quantity { get; set; }
    /// <summary>Ustama foiz = (Price - CostPrice) / CostPrice * 100.</summary>
    public decimal MarkupPercent { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsVat { get; set; }
    public bool IsMarked { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }
    public int? PluNumber { get; set; }
    public decimal? Tare { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <param name="branchId">
    /// Ko'rsatilsa — shu filialdagi qoldiq qaytariladi.
    /// null bo'lsa — barcha filiallar yig'indisi (admin view).
    /// </param>
    public static ProductDto FromEntity(Product p, Guid? branchId = null) => new()
    {
        Id = p.Id,
        Name = p.Name,
        MxikCode = p.MxikCode,
        Barcode = p.Barcode,
        CostPrice = p.CostPrice,
        Price = p.Price,
        Quantity = branchId.HasValue
            ? p.Stocks.FirstOrDefault(s => s.BranchId == branchId)?.Quantity ?? 0m
            : p.Stocks.Sum(s => s.Quantity),
        MarkupPercent = p.CostPrice > 0
            ? Math.Round((p.Price - p.CostPrice) / p.CostPrice * 100m, 2)
            : 0m,
        ImageUrl = p.ImageUrl,
        Description = p.Description,
        IsVat = p.IsVat,
        IsMarked = p.IsMarked,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name,
        Unit = p.Unit,
        IsWeighed = p.IsWeighed,
        PluNumber = p.PluNumber,
        Tare = p.Tare,
        CategoryId = p.CategoryId,
        BranchId = p.BranchId,
        BranchName = p.Branch?.Name,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
