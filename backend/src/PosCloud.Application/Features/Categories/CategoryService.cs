using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Categories.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Categories;

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
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(c => CategoryDto.FromEntity(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        await ValidateParentAsync(null, dto.ParentId, cancellationToken);
        await EnsureUniqueNameAsync(name, dto.ParentId, null, cancellationToken);

        var category = new Category
        {
            Name = name,
            Note = dto.Note?.Trim(),
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder,
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            ShowOnline = dto.ShowOnline ?? true,
        };
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
        await ValidateParentAsync(id, dto.ParentId, cancellationToken);
        await EnsureUniqueNameAsync(name, dto.ParentId, id, cancellationToken);

        category.Name = name;
        category.Note = dto.Note?.Trim();
        category.ParentId = dto.ParentId;
        category.SortOrder = dto.SortOrder;
        category.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        if (dto.ShowOnline is { } show) category.ShowOnline = show;
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

        if (await _db.Categories.AnyAsync(c => c.ParentId == id, cancellationToken))
            throw new ConflictException("Bu bo'limning ichki bo'limlari bor. Avval ularni o'chiring yoki boshqa bo'limga ko'chiring.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Daraxt ikki darajali: ota faqat asosiy bo'lim bo'ladi, ichki bo'limi bor bo'lim boshqasiga bola bo'lolmaydi.</summary>
    private async Task ValidateParentAsync(Guid? id, Guid? parentId, CancellationToken ct)
    {
        if (parentId is not { } pid) return;
        if (pid == id)
            throw new ValidationException("Kategoriya o'ziga ota bo'lolmaydi.");

        var parent = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pid, ct)
            ?? throw new NotFoundException($"Ota bo'lim topilmadi: {pid}");
        if (parent.ParentId != null)
            throw new ValidationException("Ota bo'lim asosiy bo'lim bo'lishi kerak (faqat ikki daraja).");

        if (id is { } self && await _db.Categories.AnyAsync(c => c.ParentId == self, ct))
            throw new ValidationException("Bu bo'limning ichki bo'limlari bor — uni boshqa bo'lim ichiga ko'chirib bo'lmaydi.");
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? parentId, Guid? exceptId, CancellationToken ct)
    {
        var exists = await _db.Categories.AnyAsync(
            c => c.Name == name && c.ParentId == parentId && c.Id != exceptId, ct);
        if (exists)
            throw new ConflictException($"'{name}' kategoriyasi bu bo'limda allaqachon mavjud.");
    }
}
