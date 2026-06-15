using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Dishes.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Dishes;

public class DishService : IDishService
{
    private readonly IApplicationDbContext _db;

    public DishService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DishDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dishes = await _db.Products
            .AsNoTracking()
            .Where(p => p.ProductType == ProductType.Dish)
            .Include(p => p.Category)
            .Include(p => p.Ingredients).ThenInclude(i => i.IngredientProduct)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return dishes.Select(DishDto.FromEntity).ToList();
    }

    public async Task<DishDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dish = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Ingredients).ThenInclude(i => i.IngredientProduct)
            .FirstOrDefaultAsync(p => p.Id == id && p.ProductType == ProductType.Dish, cancellationToken)
            ?? throw new NotFoundException($"Ovqat topilmadi: {id}");

        return DishDto.FromEntity(dish);
    }

    public async Task<DishDto> SetRecipeAsync(Guid dishId, SetRecipeRequest request, CancellationToken cancellationToken = default)
    {
        var isDish = await _db.Products
            .AnyAsync(p => p.Id == dishId && p.ProductType == ProductType.Dish, cancellationToken);
        if (!isDish)
            throw new NotFoundException($"Ovqat topilmadi: {dishId}");

        // Tarkibiy tovarlar mavjudligini tekshiramiz (faqat Good).
        var ingredientIds = request.Items.Select(i => i.IngredientProductId).Distinct().ToList();
        var goods = await _db.Products
            .Where(p => ingredientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Eski retseptni o'chiramiz, yangisini qo'shamiz (set semantikasi).
        var existing = await _db.DishIngredients
            .Where(i => i.DishProductId == dishId)
            .ToListAsync(cancellationToken);
        _db.DishIngredients.RemoveRange(existing);

        foreach (var item in request.Items)
        {
            if (!goods.TryGetValue(item.IngredientProductId, out var good))
                throw new NotFoundException($"Tovar topilmadi: {item.IngredientProductId}");
            if (good.ProductType != ProductType.Good)
                throw new ValidationException($"'{good.Name}' tovar emas — retseptga qo'shib bo'lmaydi.");
            if (item.Quantity <= 0)
                throw new ValidationException($"'{good.Name}' miqdori 0 dan katta bo'lishi kerak.");

            _db.DishIngredients.Add(new DishIngredient
            {
                DishProductId = dishId,
                IngredientProductId = item.IngredientProductId,
                Quantity = item.Quantity
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(dishId, cancellationToken);
    }
}
