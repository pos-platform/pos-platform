using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Categories.Dtos;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static CategoryDto FromEntity(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        CreatedAt = c.CreatedAt
    };
}

public class CreateCategoryDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
