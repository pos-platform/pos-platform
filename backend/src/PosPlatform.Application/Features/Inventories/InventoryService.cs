using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Inventories;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _db;

    public InventoryService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<InventoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var inventories = await _db.Inventories
            .AsNoTracking()
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return inventories.Select(InventoryDto.FromEntity).ToList();
    }

    public async Task<InventoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var inv = await _db.Inventories
            .AsNoTracking()
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Inventory {id} topilmadi.");

        return InventoryDto.FromEntity(inv);
    }

    public async Task<InventoryDto> CreateAsync(CreateInventoryRequest req, CancellationToken ct = default)
    {
        IQueryable<Product> query = _db.Products;

        if (req.ProductIds is { Count: > 0 })
            query = query.Where(p => req.ProductIds.Contains(p.Id));

        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        if (products.Count == 0)
            throw new ValidationException("Inventarizatsiya uchun mahsulot topilmadi.");

        var count = await _db.Inventories.CountAsync(ct);
        var number = $"INV-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";

        var inv = new Inventory
        {
            Number = number,
            Notes = req.Notes,
            BranchId = req.BranchId,
            Items = products.Select(p => new InventoryItem
            {
                ProductId = p.Id,
                ExpectedQty = p.Quantity,
                ActualQty = p.Quantity
            }).ToList()
        };

        _db.Inventories.Add(inv);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(inv.Id, ct);
    }

    public async Task<InventoryDto> UpdateActualQtyAsync(Guid id, UpdateInventoryRequest req, CancellationToken ct = default)
    {
        var inv = await _db.Inventories
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Inventory {id} topilmadi.");

        if (inv.Status != InventoryStatus.Draft)
            throw new ValidationException("Faqat Draft holatidagi inventarizatsiyani o'zgartirish mumkin.");

        var itemMap = inv.Items.ToDictionary(i => i.Id);

        foreach (var upd in req.Items)
        {
            if (!itemMap.TryGetValue(upd.ItemId, out var item))
                throw new NotFoundException($"InventoryItem {upd.ItemId} topilmadi.");

            item.ActualQty = upd.ActualQty;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<InventoryDto> ApplyAsync(Guid id, CancellationToken ct = default)
    {
        var inv = await _db.Inventories
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Inventory {id} topilmadi.");

        if (inv.Status != InventoryStatus.Draft)
            throw new ValidationException("Inventarizatsiya allaqachon qo'llanilgan.");

        var productIds = inv.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in inv.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product)) continue;

            var discrepancy = item.ActualQty - item.ExpectedQty;
            product.Quantity += discrepancy;
        }

        inv.Status = InventoryStatus.Completed;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }
}
