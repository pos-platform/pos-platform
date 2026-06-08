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
        var counts = await _db.Products
            .AsNoTracking()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(c => CategoryDto.FromEntity(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();

        var exists = await _db.Categories.AnyAsync(c => c.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException($"'{name}' kategoriyasi allaqachon mavjud.");

        var category = new Category { Name = name, Note = dto.Note?.Trim() };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return CategoryDto.FromEntity(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kategoriya topilmadi: {id}");

        var name = dto.Name.Trim();
        var exists = await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id, cancellationToken);
        if (exists)
            throw new ConflictException($"'{name}' kategoriyasi allaqachon mavjud.");

        category.Name = name;
        category.Note = dto.Note?.Trim();
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return CategoryDto.FromEntity(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kategoriya topilmadi: {id}");

        if (category.Products.Any())
            throw new ConflictException("Bu kategoriyada mahsulotlar mavjud. Avval mahsulotlarni o'chiring yoki boshqa kategoriyaga ko'chiring.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
