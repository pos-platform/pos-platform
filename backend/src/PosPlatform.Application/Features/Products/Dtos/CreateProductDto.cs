using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Products.Dtos;

public class CreateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>MXIK kodi — O'zbekiston soliq tizimi uchun majburiy.</summary>
    [Required]
    public string MxikCode { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    /// <summary>Olingan narx (tannarx).</summary>
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    /// <summary>Sotish narxi.</summary>
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public Unit Unit { get; set; } = Unit.Piece;

    public bool IsWeighed { get; set; }

    public int? PluNumber { get; set; }

    public decimal? Tare { get; set; }

    [Required]
    public Guid CategoryId { get; set; }
}
