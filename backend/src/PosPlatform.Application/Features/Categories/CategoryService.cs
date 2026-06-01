using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Categories.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Categories;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _db;

    public CategoryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(CategoryDto.FromEntity).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();

        var exists = await _db.Categories.AnyAsync(c => c.Name == name, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"'{name}' kategoriyasi allaqachon mavjud.");
        }

        var category = new Category { Name = name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return CategoryDto.FromEntity(category);
    }
}
