using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.WriteOffs.Dtos;
using PosCloud.Domain.Entities;

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
            Number = await GenerateNumberAsync(cancellationToken)
        };

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            if (!stocks.TryGetValue(line.ProductId, out var stock) || stock.Quantity < line.Quantity)
                throw new ValidationException($"'{product.Name}' qoldig'i yetarli emas (mavjud: {stock?.Quantity ?? 0}).");

            var lineTotal = Round2(line.Quantity * product.CostPrice);
            stock.Quantity -= line.Quantity;

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

        return WriteOffDto.FromEntity(writeOff);
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
