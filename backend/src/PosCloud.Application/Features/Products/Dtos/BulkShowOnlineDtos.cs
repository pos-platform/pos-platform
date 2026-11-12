using System.ComponentModel.DataAnnotations;

namespace PosCloud.Application.Features.Products.Dtos;

/// <summary>Bir nechta mahsulotni ilovada ko'rsatish/yashirish (ko'pi bilan 1000 ta).</summary>
public class BulkShowOnlineRequest
{
    [Required]
    [MaxLength(1000)]
    public List<Guid> ProductIds { get; set; } = [];

    public bool ShowOnline { get; set; }
}

/// <summary>Haqiqatan o'zgargan mahsulotlar soni.</summary>
public record BulkShowOnlineResult(int Updated);
