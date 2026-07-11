using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Products.Dtos;

/// <summary>
/// Kassa (desktop POS) uchun yengillashtirilgan mahsulot DTO'si.
/// Faqat kassa ishlatadigan maydonlar — Description, MarkingCode, tannarx,
/// supplier/branch nomi va vaqt tamg'alari kabi og'ir/keraksiz maydonlarsiz.
/// SQL'da to'g'ridan-to'g'ri proyeksiya qilinadi (JOIN'siz), shu sabab so'rov ham,
/// tarmoq yuki ham ancha yengil bo'ladi.
/// </summary>
public class PosProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string MxikCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }
    public int? PluNumber { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
}
