using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Products.Dtos;

public class CreateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tovar (Good) yoki ovqat (Dish). Restoran rejimida ovqatlar uchun Dish.</summary>
    public ProductType ProductType { get; set; } = ProductType.Good;

    /// <summary>Tayyorlash vaqti (daqiqa) — ovqat uchun ixtiyoriy.</summary>
    public int? PrepMinutes { get; set; }

    /// <summary>MXIK kodi. Bo'sh bo'lsa "00000000" ishlatiladi.</summary>
    public string? MxikCode { get; set; }

    public string? Barcode { get; set; }

    /// <summary>Olingan narx (tannarx).</summary>
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    /// <summary>Sotish narxi.</summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    /// <summary>QQS (НДС) bilanmi.</summary>
    public bool IsVat { get; set; } = true;

    /// <summary>Markirovka (маркировка).</summary>
    public bool IsMarked { get; set; }

    /// <summary>Markirovka kodi (Код маркировки) — ixtiyoriy.</summary>
    public string? MarkingCode { get; set; }

    /// <summary>Qadoqdagi kod (Код на упаковке) — ixtiyoriy.</summary>
    public string? PackageCode { get; set; }

    /// <summary>Komitent STIR (9 xona) yoki JSHSHIR (14 xona) — komissiya tovari uchun, ixtiyoriy.</summary>
    [RegularExpression(@"^\s*(\d{9}|\d{14})?\s*$", ErrorMessage = "Komitent STIR 9 yoki JSHSHIR 14 raqamdan iborat bo'lishi kerak.")]
    public string? CommitentTin { get; set; }

    public Guid? SupplierId { get; set; }

    /// <summary>Boshlang'ich ombor qoldig'i.</summary>
    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    public Unit Unit { get; set; } = Unit.Piece;

    public bool IsWeighed { get; set; }

    public int? PluNumber { get; set; }

    public decimal? Tare { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    /// <summary>Mahsulot tegishli do'kon. null = barcha do'konlarga umumiy.</summary>
    public Guid? BranchId { get; set; }
}

/// <summary>Mahsulotni tahrirlash (yaratish bilan bir xil maydonlar).</summary>
public class UpdateProductDto : CreateProductDto
{
}
