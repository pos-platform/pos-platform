using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.WriteOffs.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.WriteOffs;

public class WriteOffService : IWriteOffService
{
    private readonly IApplicationDbContext _db;

    public WriteOffService(IApplicationDbContext db)
    {
        _db = db;
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
        {
            throw new ValidationException("Hisobdan chiqarishda kamida bitta tovar bo'lishi kerak.");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var writeOff = new WriteOff
        {
            BranchId = request.BranchId,
            Reason = request.Reason?.Trim(),
            Number = await GenerateNumberAsync(cancellationToken)
        };

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            }
            if (line.Quantity <= 0)
            {
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");
            }

            var lineTotal = Round2(line.Quantity * product.CostPrice);

            // Ombor qoldig'idan ayiramiz.
            product.Quantity -= line.Quantity;

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
