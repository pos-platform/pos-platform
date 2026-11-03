using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Promotions.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Promotions;

public class PromotionService : IPromotionService
{
    private readonly IApplicationDbContext _db;

    public PromotionService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PromotionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var promotions = await _db.Promotions
            .AsNoTracking()
            .Include(p => p.Product)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return promotions.Select(p => PromotionDto.FromEntity(p, now)).ToList();
    }

    public async Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var promotions = await _db.Promotions
            .AsNoTracking()
            .Include(p => p.Product)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync(cancellationToken);

        return promotions.Select(p => PromotionDto.FromEntity(p, now)).ToList();
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductId is not null && request.CategoryId is not null)
        {
            throw new ValidationException("Aksiya bir vaqtda mahsulot va kategoriyaga bog'lanmasligi kerak.");
        }
        if (request.EndDate < request.StartDate)
        {
            throw new ValidationException("Tugash sanasi boshlanish sanasidan oldin bo'lmasligi kerak.");
        }
        if (request.ProductId is not null &&
            !await _db.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
        {
            throw new NotFoundException($"Mahsulot topilmadi: {request.ProductId}");
        }
        if (request.CategoryId is not null &&
            !await _db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new NotFoundException($"Kategoriya topilmadi: {request.CategoryId}");
        }

        var promotion = new Promotion
        {
            Name = request.Name.Trim(),
            DiscountPercent = request.DiscountPercent,
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            IsActive = true
        };

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdDtoAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Aksiya topilmadi: {id}");

        promotion.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdDtoAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Aksiya topilmadi: {id}");

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PromotionDto> GetByIdDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var promotion = await _db.Promotions
            .AsNoTracking()
            .Include(p => p.Product)
            .Include(p => p.Category)
            .FirstAsync(p => p.Id == id, cancellationToken);

        return PromotionDto.FromEntity(promotion, now);
    }
}
