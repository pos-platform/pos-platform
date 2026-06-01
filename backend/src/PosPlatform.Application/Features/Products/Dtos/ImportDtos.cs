namespace PosPlatform.Application.Features.Products.Dtos;

/// <summary>Excel'dan o'qilgan bitta qator (xom qiymatlar).</summary>
public class ProductImportRow
{
    public int RowNumber { get; set; }
    public string? Name { get; set; }
    public string? MxikCode { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? CategoryName { get; set; }
    public bool? IsVat { get; set; }
}

/// <summary>Eksport uchun qator.</summary>
public class ProductExportRow
{
    public string Name { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class ImportResultDto
{
    public int Created { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
