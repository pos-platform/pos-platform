using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Stock;

public class StockReceiptService : IStockReceiptService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StockReceiptService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StockReceiptDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var receipts = await _db.StockReceipts
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Branch)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return receipts.Select(StockReceiptDto.FromEntity).ToList();
    }

    public async Task<StockReceiptDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await _db.StockReceipts
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Branch)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt is null)
            throw new NotFoundException($"Накладная topilmadi: {id}");

        return StockReceiptDto.FromEntity(receipt);
    }

    public async Task<StockReceiptDto> CreateAsync(CreateStockReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("Kirimda kamida bitta tovar bo'lishi kerak.");

        var branchId = request.BranchId ?? _currentUser.BranchId
            ?? throw new ValidationException("Kirim uchun filial ko'rsatilmagan.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new NotFoundException($"Yetkazib beruvchi topilmadi: {request.SupplierId}");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Filial qoldiqlarini oldindan yuklaymiz (tracking uchun)
        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var receipt = new StockReceipt
        {
            SupplierId = supplier.Id,
            BranchId = branchId,
            Comment = request.Comment?.Trim(),
            Number = await GenerateNumberAsync(cancellationToken)
        };

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            var lineTotal = Round2(line.Quantity * line.CostPrice);

            // Filial qoldig'ini oshiramiz
            if (!stocks.TryGetValue(line.ProductId, out var stock))
            {
                stock = new BranchStock { BranchId = branchId, ProductId = product.Id };
                _db.BranchStocks.Add(stock);
                stocks[line.ProductId] = stock;
            }
            stock.Quantity += line.Quantity;

            if (line.CostPrice > 0)
                product.CostPrice = line.CostPrice;

            receipt.Items.Add(new StockReceiptItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                CostPrice = line.CostPrice,
                RemainingQuantity = line.Quantity, // FIFO qatlami — boshида to'liq qoldiq
                LineTotal = lineTotal,
                BatchNumber = line.BatchNumber?.Trim(),
                ExpiryDate = line.ExpiryDate
            });
        }

        receipt.TotalAmount = receipt.Items.Sum(i => i.LineTotal);
        supplier.Balance += receipt.TotalAmount;

        _db.StockReceipts.Add(receipt);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(receipt.Id, cancellationToken);
    }

    private async Task<string> GenerateNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await _db.StockReceipts
            .CountAsync(r => r.CreatedAt >= today && r.CreatedAt < tomorrow, cancellationToken);
        return $"N-{today:yyyyMMdd}-{count + 1:D4}";
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
