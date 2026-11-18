using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Inventories;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public InventoryService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

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
        // Xizmat (yetkazish) mahsuloti omborda yo'q — inventarizatsiyaga kirmaydi.
        IQueryable<Product> query = _db.Products.Where(p => p.ProductType != Domain.Enums.ProductType.Service);

        if (req.ProductIds is { Count: > 0 })
            query = query.Where(p => req.ProductIds.Contains(p.Id));

        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        if (products.Count == 0)
            throw new ValidationException("Inventarizatsiya uchun mahsulot topilmadi.");

        // Filial qoldiqlari (agar BranchId ko'rsatilgan bo'lsa)
        Dictionary<Guid, decimal> stockMap = new();
        if (req.BranchId.HasValue)
        {
            var productIds = products.Select(p => p.Id).ToList();
            stockMap = await _db.BranchStocks
                .Where(s => s.BranchId == req.BranchId && productIds.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, ct);
        }

        var count = await _db.Inventories.CountAsync(ct);
        var number = $"INV-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";

        var inv = new Inventory
        {
            Number = number,
            Notes = req.Notes,
            BranchId = req.BranchId,
            Items = products.Select(p =>
            {
                var qty = stockMap.TryGetValue(p.Id, out var q) ? q : 0m;
                return new InventoryItem
                {
                    ProductId = p.Id,
                    ExpectedQty = qty,
                    ActualQty = qty
                };
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

        // Sanalgan miqdor o'zgargan qatorlar uchun tizim qoldig'ini AYNAN HOZIR qayta olamiz:
        // hujjat ochilgandan beri sotilgan tovar farqqa ikkinchi marta tushmasin.
        var changed = req.Items
            .Where(u => itemMap.TryGetValue(u.ItemId, out var it) && it.ActualQty != u.ActualQty)
            .Select(u => itemMap[u.ItemId].ProductId)
            .Distinct()
            .ToList();
        var current = inv.BranchId.HasValue && changed.Count > 0
            ? await _db.BranchStocks.AsNoTracking()
                .Where(s => s.BranchId == inv.BranchId && changed.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, ct)
            : new Dictionary<Guid, decimal>();

        foreach (var upd in req.Items)
        {
            if (!itemMap.TryGetValue(upd.ItemId, out var item))
                throw new NotFoundException($"InventoryItem {upd.ItemId} topilmadi.");
            if (item.ActualQty == upd.ActualQty) continue;

            if (inv.BranchId.HasValue)
                item.ExpectedQty = current.TryGetValue(item.ProductId, out var q) ? q : 0m;
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

        if (!inv.BranchId.HasValue)
            throw new ValidationException("Inventarizatsiya filialga biriktirilmagan.");

        var productIds = inv.Items.Select(i => i.ProductId).Distinct().ToList();

        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == inv.BranchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, ct);

        // Farq = sanalgan miqdor − SANASH paytidagi tizim qoldig'i (UpdateActualQtyAsync'da olinadi).
        // Sanashdan keyingi sotuvlar qoldiqda allaqachon aks etgan — farq ularning ustiga qo'shiladi.
        var doc = new StockDoc(inv.Id, inv.Number, _currentUser.UserId);
        foreach (var item in inv.Items)
        {
            var discrepancy = item.ActualQty - item.ExpectedQty;
            if (discrepancy == 0) continue;

            var stock = StockLedger.GetOrCreate(_db, stocks, inv.BranchId.Value, item.ProductId);
            StockLedger.Apply(_db, stock, discrepancy, StockMovementType.Inventory, doc);
        }

        inv.Status = InventoryStatus.Completed;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }
}
