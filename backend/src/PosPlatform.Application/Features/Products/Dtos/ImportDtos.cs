namespace PosPlatform.Application.Features.Products.Dtos;

/// <summary>
/// Excel'dan o'qilgan bitta mahsulot qatori.
/// Ustun tartibi skrenshottagi formatga mos:
/// A(№) B(Nomi) C(Miqdor) D(Barcode) E(PLU) F(Tip:шт/кг)
/// G(Tannarx) H(Narx) I(Naценka%) J(Do'kon) K(Kategoriya)
/// L(Chegirma bor?) M(Chegirmali narx) N(НДС) O(Birlik) P(MXIK/ИКПУ)
/// Q(IsMarked) R(Код упаковки)
/// </summary>
public class ProductImportRow
{
    public int RowNumber { get; set; }
    public string? Name { get; set; }           // B
    public decimal? Quantity { get; set; }      // C
    public string? Barcode { get; set; }        // D
    public int? PluNumber { get; set; }         // E
    public string? UnitType { get; set; }       // F (шт/кг)
    public decimal? CostPrice { get; set; }     // G
    public decimal? Price { get; set; }         // H
    public decimal? MarkupPercent { get; set; } // I
    public string? BranchName { get; set; }     // J
    public string? CategoryName { get; set; }   // K
    public bool? HasDiscount { get; set; }      // L (ha/нет)
    public decimal? DiscountPrice { get; set; } // M
    public bool? IsVat { get; set; }            // N
    public string? Unit { get; set; }           // O (Dona/Kg)
    public string? MxikCode { get; set; }       // P
    public bool? IsMarked { get; set; }         // Q
    public string? PackageCode { get; set; }    // R
}

/// <summary>Eksport uchun qator — skrenshottagi barcha ustunlar.</summary>
public class ProductExportRow
{
    public string Name { get; set; } = string.Empty;       // B
    public decimal Quantity { get; set; }                  // C
    public string? Barcode { get; set; }                   // D
    public int? PluNumber { get; set; }                    // E
    public string UnitType { get; set; } = "шт";          // F
    public decimal CostPrice { get; set; }                 // G
    public decimal Price { get; set; }                     // H
    public decimal MarkupPercent { get; set; }             // I
    public string? BranchName { get; set; }                // J
    public string Category { get; set; } = string.Empty;  // K
    public string HasDiscount { get; set; } = "нет";      // L
    public decimal? DiscountPrice { get; set; }            // M
    public string IsVat { get; set; } = "нет";            // N
    public string Unit { get; set; } = string.Empty;      // O
    public string MxikCode { get; set; } = string.Empty;  // P
    public string IsMarked { get; set; } = "нет";         // Q
    public string? PackageCode { get; set; }               // R
}

public class ImportResultDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
