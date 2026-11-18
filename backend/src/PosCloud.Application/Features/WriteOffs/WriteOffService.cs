using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock;
using PosCloud.Application.Features.WriteOffs.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.WriteOffs;

public class WriteOffService : IWriteOffService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WriteOffService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WriteOffDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var writeOffs = await _db.WriteOffs
            .AsNoTracking()
            .Include(w => w.Items).ThenInclude(i => i.Product)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return writeOffs.Select(WriteOffDto.FromEntity).ToList();
    }

    public async Task<WriteOffDto> CreateAsync(CreateWriteOffRequest request, CancellationToken cancellationToken = default)
    {
        // Ikki marta saqlanishdan himoya: shu kalit bilan hujjat bo'lsa — o'shani qaytaramiz.
        var clientRequestId = string.IsNullOrWhiteSpace(request.ClientRequestId) ? null : request.ClientRequestId.Trim();
        if (clientRequestId is not null)
        {
            var existing = await LoadAsync(w => w.ClientRequestId == clientRequestId, cancellationToken);
            if (existing is not null) return existing;
        }

        if (request.Items.Count == 0)
            throw new ValidationException("Hisobdan chiqarishda kamida bitta tovar bo'lishi kerak.");

        var branchId = request.BranchId ?? _currentUser.BranchId
            ?? throw new ValidationException("Hisobdan chiqarish uchun filial ko'rsatilmagan.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var writeOff = new WriteOff
        {
            BranchId = branchId,
            Reason = request.Reason?.Trim(),
            ClientRequestId = clientRequestId,
            Number = await GenerateNumberAsync(cancellationToken)
        };
        var doc = new StockDoc(writeOff.Id, writeOff.Number, _currentUser.UserId);

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            // Online buyurtmalarga band qilingan tovar hisobdan chiqarilmaydi.
            stocks.TryGetValue(line.ProductId, out var stock);
            if (stock is null || StockLedger.Available(stock) < line.Quantity)
                throw new ValidationException(
                    $"'{product.Name}' qoldig'i yetarli emas (mavjud: {StockLedger.Available(stock):0.###}" +
                    (stock is { Reserved: > 0 } ? $", online buyurtmalarga band: {stock.Reserved:0.###}" : "") + ").");

            var lineTotal = Round2(line.Quantity * product.CostPrice);
            StockLedger.Apply(_db, stock, -line.Quantity, StockMovementType.WriteOff, doc, product.CostPrice);

            writeOff.Items.Add(new WriteOffItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                CostPrice = product.CostPrice,
                LineTotal = lineTotal
            });
        }

        writeOff.TotalCost = writeOff.Items.Sum(i => i.LineTotal);

        _db.WriteOffs.Add(writeOff);
        await _db.SaveChangesAsync(cancellationToken);

        return (await LoadAsync(w => w.Id == writeOff.Id, cancellationToken))!;
    }

    /// <summary>Hisobdan chiqarishni bekor qiladi (storno): tovar omborga qaytadi.</summary>
    public async Task<WriteOffDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var writeOff = await _db.WriteOffs
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Hisobdan chiqarish topilmadi: {id}");

        if (writeOff.Status == StockDocumentStatus.Cancelled)
            throw new ValidationException("Hujjat allaqachon bekor qilingan.");
        var branchId = writeOff.BranchId
            ?? throw new ValidationException("Hujjat filialga biriktirilmagan — bekor qilib bo'lmaydi.");

        var productIds = writeOff.Items.Select(i => i.ProductId).Distinct().ToList();
        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var doc = new StockDoc(writeOff.Id, writeOff.Number, _currentUser.UserId);
        foreach (var item in writeOff.Items)
        {
            var stock = StockLedger.GetOrCreate(_db, stocks, branchId, item.ProductId);
            StockLedger.Apply(_db, stock, item.Quantity, StockMovementType.WriteOffCancel, doc, item.CostPrice);
        }

        writeOff.Status = StockDocumentStatus.Cancelled;
        writeOff.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (await LoadAsync(w => w.Id == writeOff.Id, cancellationToken))!;
    }

    private async Task<WriteOffDto?> LoadAsync(System.Linq.Expressions.Expression<Func<WriteOff, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var w = await _db.WriteOffs
            .AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(predicate, cancellationToken);
        return w is null ? null : WriteOffDto.FromEntity(w);
    }

    private async Task<string> GenerateNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await _db.WriteOffs
            .CountAsync(w => w.CreatedAt >= today && w.CreatedAt < tomorrow, cancellationToken);
        return $"W-{today:yyyyMMdd}-{count + 1:D4}";
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
