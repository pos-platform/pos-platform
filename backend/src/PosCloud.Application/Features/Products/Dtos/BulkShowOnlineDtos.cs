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

/// <summary>Bir nechta mahsulotni boshqa kategoriyaga ko'chirish (ko'pi bilan 1000 ta).</summary>
public class BulkCategoryRequest
{
    [Required]
    [MaxLength(1000)]
    public List<Guid> ProductIds { get; set; } = [];

    public Guid CategoryId { get; set; }
}

/// <summary>Mahsulot rasmlarini ommaviy yozish (ko'pi bilan 1000 ta). OnlyIfEmpty — faqat rasmi yo'qlarga.</summary>
public class BulkImageRequest
{
    [Required]
    [MaxLength(1000)]
    public List<BulkImageItem> Items { get; set; } = [];

    public bool OnlyIfEmpty { get; set; } = true;
}

public class BulkImageItem
{
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(512)]
    public string ImageUrl { get; set; } = string.Empty;
}
