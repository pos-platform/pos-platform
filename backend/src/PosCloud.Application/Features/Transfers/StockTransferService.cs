using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock;
using PosCloud.Application.Features.Transfers.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Transfers;

public class StockTransferService : IStockTransferService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StockTransferService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StockTransferDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transfers = await _db.StockTransfers
            .AsNoTracking()
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return transfers.Select(StockTransferDto.FromEntity).ToList();
    }

    public async Task<StockTransferDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await LoadAsync(id, cancellationToken);
        return StockTransferDto.FromEntity(transfer);
    }

    public async Task<StockTransferDto> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Ikki marta saqlanishdan himoya: shu kalit bilan transfer bo'lsa — o'shani qaytaramiz.
        var clientRequestId = string.IsNullOrWhiteSpace(request.ClientRequestId) ? null : request.ClientRequestId.Trim();
        if (clientRequestId is not null)
        {
            var existingId = await _db.StockTransfers.Where(t => t.ClientRequestId == clientRequestId)
                .Select(t => (Guid?)t.Id).FirstOrDefaultAsync(cancellationToken);
            if (existingId is { } eid) return await GetByIdAsync(eid, cancellationToken);
        }

        if (request.FromBranchId == request.ToBranchId)
            throw new ValidationException("Manba va maqsad filial bir xil bo'lmasligi kerak.");

        // Filialga biriktirilgan foydalanuvchi faqat o'z filialidan jo'nata oladi; ikkala filial shu tashkilotniki.
        await _currentUser.ResolveForWriteAsync(_db, request.FromBranchId, cancellationToken);
        if (!await _db.Branches.AnyAsync(b => b.Id == request.ToBranchId, cancellationToken))
            throw new ValidationException("Qabul qiluvchi filial topilmadi.");

        if (request.Items.Count == 0)
            throw new ValidationException("Transferda kamida bitta tovar bo'lishi kerak.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Manba filialning qoldiqlarini yuklaymiz
        var fromStocks = await _db.BranchStocks
            .Where(s => s.BranchId == request.FromBranchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var transfer = new StockTransfer
        {
            FromBranchId = request.FromBranchId,
            ToBranchId = request.ToBranchId,
            Comment = request.Comment?.Trim(),
            Status = TransferStatus.Pending,
            ClientRequestId = clientRequestId,
            Number = await GenerateNumberAsync(cancellationToken)
        };
        var doc = new StockDoc(transfer.Id, transfer.Number, _currentUser.UserId);

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            // Online buyurtmalarga band qilingan tovar boshqa filialga jo'natilmaydi.
            fromStocks.TryGetValue(line.ProductId, out var fromStock);
            if (fromStock is null || StockLedger.Available(fromStock) < line.Quantity)
                throw new ValidationException(
                    $"'{product.Name}' uchun qoldiq yetarli emas (mavjud: {StockLedger.Available(fromStock):0.###}" +
                    (fromStock is { Reserved: > 0 } ? $", online buyurtmalarga band: {fromStock.Reserved:0.###}" : "") + ").");

            // Manba filialdan ayiramiz
            StockLedger.Apply(_db, fromStock, -line.Quantity, StockMovementType.TransferOut, doc, product.CostPrice);

            transfer.Items.Add(new StockTransferItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                CostPrice = product.CostPrice,
                LineTotal = Round2(line.Quantity * product.CostPrice)
            });
        }

        transfer.TotalCost = transfer.Items.Sum(i => i.LineTotal);
        transfer.SentAt = DateTime.UtcNow;
        transfer.Status = TransferStatus.Sent;

        _db.StockTransfers.Add(transfer);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(transfer.Id, cancellationToken);
    }

    public async Task<StockTransferDto> ReceiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _db.StockTransfers
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Transfer topilmadi: {id}");

        if (transfer.Status != TransferStatus.Sent)
            throw new ValidationException("Faqat 'Yuborilgan' statusdagi transferni qabul qilish mumkin.");

        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();

        // Maqsad filialning mavjud qoldiqlarini yuklaymiz
        var toStocks = await _db.BranchStocks
            .Where(s => s.BranchId == transfer.ToBranchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var doc = new StockDoc(transfer.Id, transfer.Number, _currentUser.UserId);
        foreach (var item in transfer.Items)
        {
            var toStock = StockLedger.GetOrCreate(_db, toStocks, transfer.ToBranchId, item.ProductId);
            StockLedger.Apply(_db, toStock, item.Quantity, StockMovementType.TransferIn, doc, item.CostPrice);
        }

        transfer.Status = TransferStatus.Received;
        transfer.ReceivedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<StockTransferDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _db.StockTransfers
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Transfer topilmadi: {id}");

        if (transfer.Status == TransferStatus.Received)
            throw new ValidationException("Qabul qilingan transferni bekor qilib bo'lmaydi.");
        if (transfer.Status == TransferStatus.Cancelled)
            throw new ValidationException("Transfer allaqachon bekor qilingan.");

        // Yuborilgan bo'lsa — manba filialga qaytaramiz
        if (transfer.Status == TransferStatus.Sent)
        {
            var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();
            var fromStocks = await _db.BranchStocks
                .Where(s => s.BranchId == transfer.FromBranchId && productIds.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, cancellationToken);

            var doc = new StockDoc(transfer.Id, transfer.Number, _currentUser.UserId);
            foreach (var item in transfer.Items)
            {
                var fromStock = StockLedger.GetOrCreate(_db, fromStocks, transfer.FromBranchId, item.ProductId);
                StockLedger.Apply(_db, fromStock, item.Quantity, StockMovementType.TransferCancel, doc, item.CostPrice);
            }
        }

        transfer.Status = TransferStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<StockTransfer> LoadAsync(Guid id, CancellationToken ct) =>
        await _db.StockTransfers
            .AsNoTracking()
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException($"Transfer topilmadi: {id}");

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var count = await _db.StockTransfers.CountAsync(t => t.CreatedAt >= today, ct);
        return $"TR-{today:yyyyMMdd}-{count + 1:D4}";
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
