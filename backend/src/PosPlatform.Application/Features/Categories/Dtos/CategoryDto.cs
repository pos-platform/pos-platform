using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Categories.Dtos;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CategoryDto FromEntity(Category c, int productCount = 0) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Note = c.Note,
        ProductCount = productCount,
        CreatedAt = c.CreatedAt
    };
}

public class CreateCategoryDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class UpdateCategoryDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
}
