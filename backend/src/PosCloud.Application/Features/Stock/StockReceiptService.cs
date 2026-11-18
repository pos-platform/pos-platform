using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

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
        // Ikki marta saqlanishdan himoya: shu kalit bilan kirim allaqachon bo'lsa — o'shani qaytaramiz.
        var clientRequestId = string.IsNullOrWhiteSpace(request.ClientRequestId) ? null : request.ClientRequestId.Trim();
        if (clientRequestId is not null)
        {
            var existingId = await _db.StockReceipts.Where(r => r.ClientRequestId == clientRequestId)
                .Select(r => (Guid?)r.Id).FirstOrDefaultAsync(cancellationToken);
            if (existingId is { } eid) return await GetByIdAsync(eid, cancellationToken);
        }

        if (request.Items.Count == 0)
            throw new ValidationException("Kirimda kamida bitta tovar bo'lishi kerak.");

        // Filialga biriktirilgan foydalanuvchi faqat o'z filialiga kirim qiladi; filial shu tashkilotniki bo'lishi shart.
        var branchId = await _currentUser.ResolveForWriteAsync(_db, request.BranchId, cancellationToken)
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
            ClientRequestId = clientRequestId,
            Number = await GenerateNumberAsync(cancellationToken)
        };
        var doc = new StockDoc(receipt.Id, receipt.Number, _currentUser.UserId);

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            var lineTotal = Round2(line.Quantity * line.CostPrice);

            // Filial qoldig'ini oshiramiz (harakat jurnaliga yoziladi)
            var stock = StockLedger.GetOrCreate(_db, stocks, branchId, product.Id);
            StockLedger.Apply(_db, stock, line.Quantity, StockMovementType.Receipt, doc, line.CostPrice);

            if (line.CostPrice > 0)
                product.CostPrice = line.CostPrice;

            receipt.Items.Add(new StockReceiptItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                CostPrice = line.CostPrice,
                RemainingQuantity = line.Quantity, // FIFO qatlami — boshida to'liq qoldiq
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

    /// <summary>
    /// Kirimni bekor qiladi (storno): tovar ombordan ayiriladi, partiya yopiladi,
    /// yetkazib beruvchi qarzi kamayadi. Partiyadan sotilgan yoki qoldiq yetmasa — rad etiladi.
    /// </summary>
    public async Task<StockReceiptDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await _db.StockReceipts
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Накладная topilmadi: {id}");

        if (receipt.Status == StockDocumentStatus.Cancelled)
            throw new ValidationException("Kirim allaqachon bekor qilingan.");
        var branchId = receipt.BranchId
            ?? throw new ValidationException("Kirim filialga biriktirilmagan — bekor qilib bo'lmaydi.");

        var consumed = receipt.Items.FirstOrDefault(i => i.RemainingQuantity < i.Quantity);
        if (consumed is not null)
            throw new ValidationException(
                $"'{consumed.Product?.Name}' bu kirim partiyasidan allaqachon sotilgan — kirimni bekor qilib bo'lmaydi. " +
                "Ortiqcha tovarni hisobdan chiqarish orqali to'g'rilang.");

        var productIds = receipt.Items.Select(i => i.ProductId).Distinct().ToList();
        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        foreach (var g in receipt.Items.GroupBy(i => i.ProductId))
        {
            var qty = g.Sum(i => i.Quantity);
            stocks.TryGetValue(g.Key, out var stock);
            if (StockLedger.Available(stock) < qty)
                throw new ValidationException(
                    $"'{g.First().Product?.Name}' qoldig'i yetarli emas (mavjud: {StockLedger.Available(stock):0.###}, kerak: {qty:0.###}) — kirimni bekor qilib bo'lmaydi.");
        }

        var doc = new StockDoc(receipt.Id, receipt.Number, _currentUser.UserId);
        foreach (var item in receipt.Items)
        {
            StockLedger.Apply(_db, stocks[item.ProductId], -item.Quantity, StockMovementType.ReceiptCancel, doc, item.CostPrice);
            item.RemainingQuantity = 0;
        }

        if (receipt.Supplier is not null)
            receipt.Supplier.Balance -= receipt.TotalAmount;

        receipt.Status = StockDocumentStatus.Cancelled;
        receipt.CancelledAt = DateTime.UtcNow;
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
