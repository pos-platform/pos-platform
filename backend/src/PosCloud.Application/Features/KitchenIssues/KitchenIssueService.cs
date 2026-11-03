using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.KitchenIssues.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.KitchenIssues;

public class KitchenIssueService : IKitchenIssueService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public KitchenIssueService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<KitchenIssueDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var issues = await _db.KitchenIssues
            .AsNoTracking()
            .Include(k => k.ChefUser)
            .Include(k => k.Items).ThenInclude(i => i.IngredientProduct)
            .Include(k => k.Items).ThenInclude(i => i.DishProduct)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return issues.Select(KitchenIssueDto.FromEntity).ToList();
    }

    public async Task<KitchenIssueDto> CreateAsync(CreateKitchenIssueRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("Oshpazga berishda kamida bitta tovar bo'lishi kerak.");

        var branchId = request.BranchId ?? _currentUser.BranchId
            ?? throw new ValidationException("Oshpazga berish uchun filial ko'rsatilmagan.");

        var productIds = request.Items.Select(i => i.IngredientProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var issue = new KitchenIssue
        {
            BranchId = branchId,
            ChefUserId = request.ChefUserId,
            Comment = request.Comment?.Trim(),
            Number = await GenerateNumberAsync(cancellationToken)
        };

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.IngredientProductId, out var product))
                throw new NotFoundException($"Tovar topilmadi: {line.IngredientProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");
            if (!stocks.TryGetValue(line.IngredientProductId, out var stock) || stock.Quantity < line.Quantity)
                throw new ValidationException($"'{product.Name}' qoldig'i yetarli emas (mavjud: {stock?.Quantity ?? 0}).");

            var lineTotal = Round2(line.Quantity * product.CostPrice);
            stock.Quantity -= line.Quantity;

            issue.Items.Add(new KitchenIssueItem
            {
                DishProductId = line.DishProductId,
                IngredientProductId = product.Id,
                Quantity = line.Quantity,
                CostPrice = product.CostPrice,
                LineTotal = lineTotal
            });
        }

        issue.TotalCost = issue.Items.Sum(i => i.LineTotal);

        _db.KitchenIssues.Add(issue);
        await _db.SaveChangesAsync(cancellationToken);

        return KitchenIssueDto.FromEntity(issue);
    }

    public async Task<IReadOnlyList<SuggestedIssueItemDto>> GetSuggestedItemsAsync(Guid dishId, decimal portions, CancellationToken cancellationToken = default)
    {
        if (portions <= 0) portions = 1;

        var ingredients = await _db.DishIngredients
            .AsNoTracking()
            .Where(i => i.DishProductId == dishId)
            .Include(i => i.IngredientProduct)
            .ToListAsync(cancellationToken);

        return ingredients.Select(i => new SuggestedIssueItemDto
        {
            IngredientProductId = i.IngredientProductId,
            IngredientName = i.IngredientProduct?.Name,
            Quantity = Round3(i.Quantity * portions)
        }).ToList();
    }

    public async Task<IReadOnlyList<KitchenConsumptionRow>> GetConsumptionReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _db.KitchenIssueItems
            .AsNoTracking()
            .Include(i => i.IngredientProduct)
            .Include(i => i.DishProduct)
            .Include(i => i.KitchenIssue)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(i => i.KitchenIssue!.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(i => i.KitchenIssue!.CreatedAt < to.Value);

        var items = await query.ToListAsync(cancellationToken);

        return items
            .GroupBy(i => new { i.DishProductId, DishName = i.DishProduct?.Name, i.IngredientProductId, IngredientName = i.IngredientProduct?.Name })
            .Select(g => new KitchenConsumptionRow
            {
                DishProductId = g.Key.DishProductId,
                DishName = g.Key.DishName,
                IngredientProductId = g.Key.IngredientProductId,
                IngredientName = g.Key.IngredientName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalCost = g.Sum(x => x.LineTotal)
            })
            .OrderBy(r => r.DishName).ThenBy(r => r.IngredientName)
            .ToList();
    }

    private async Task<string> GenerateNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await _db.KitchenIssues
            .CountAsync(k => k.CreatedAt >= today && k.CreatedAt < tomorrow, cancellationToken);
        return $"K-{today:yyyyMMdd}-{count + 1:D4}";
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal Round3(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
