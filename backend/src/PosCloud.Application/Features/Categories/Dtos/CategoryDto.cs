using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Categories.Dtos;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
    public bool ShowOnline { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CategoryDto FromEntity(Category c, int productCount = 0) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Note = c.Note,
        ParentId = c.ParentId,
        SortOrder = c.SortOrder,
        ImageUrl = c.ImageUrl,
        ShowOnline = c.ShowOnline,
        ProductCount = productCount,
        CreatedAt = c.CreatedAt
    };
}

public class CreateCategoryDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }

    /// <summary>Ota bo'lim (faqat asosiy bo'lim bo'lishi mumkin). Null — asosiy bo'lim.</summary>
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(512)]
    public string? ImageUrl { get; set; }

    /// <summary>Null — o'zgartirilmaydi (yaratishda true).</summary>
    public bool? ShowOnline { get; set; }
}

public class UpdateCategoryDto : CreateCategoryDto
{
}
