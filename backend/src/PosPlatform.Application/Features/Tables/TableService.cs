using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Tables.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Tables;

public class TableService : ITableService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TableService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TableDto>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        // branchId berilmasa va foydalanuvchi filialga biriktirilgan bo'lsa (kassir/ofitsiant) — o'sha filial.
        branchId ??= _currentUser.BranchId;

        var query = _db.RestaurantTables.AsNoTracking().AsQueryable();
        if (branchId.HasValue)
            query = query.Where(t => t.BranchId == branchId || t.BranchId == null);

        var tables = await query
            .OrderBy(t => t.Hall).ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // Ochiq buyurtmalar (Pending Sale) bo'yicha stol holatini boyitamiz.
        var tableIds = tables.Select(t => t.Id).ToList();
        var openOrders = await _db.Sales
            .AsNoTracking()
            .Where(s => s.Status == SaleStatus.Pending && s.TableId != null && tableIds.Contains(s.TableId!.Value))
            .Select(s => new { s.Id, s.TableId, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var byTable = openOrders.GroupBy(o => o.TableId!.Value).ToDictionary(g => g.Key, g => g.First());

        return tables.Select(t =>
        {
            var dto = TableDto.FromEntity(t);
            if (byTable.TryGetValue(t.Id, out var order))
            {
                dto.OpenOrderId = order.Id;
                dto.OpenOrderTotal = order.TotalAmount;
            }
            return dto;
        }).ToList();
    }

    public async Task<TableDto> CreateAsync(CreateTableDto dto, CancellationToken cancellationToken = default)
    {
        var table = new RestaurantTable
        {
            Name = dto.Name.Trim(),
            Hall = string.IsNullOrWhiteSpace(dto.Hall) ? null : dto.Hall.Trim(),
            Seats = dto.Seats,
            BranchId = dto.BranchId ?? _currentUser.BranchId,
            Status = TableStatus.Free
        };
        _db.RestaurantTables.Add(table);
        await _db.SaveChangesAsync(cancellationToken);
        return TableDto.FromEntity(table);
    }

    public async Task<TableDto> UpdateAsync(Guid id, UpdateTableDto dto, CancellationToken cancellationToken = default)
    {
        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Stol topilmadi: {id}");

        table.Name = dto.Name.Trim();
        table.Hall = string.IsNullOrWhiteSpace(dto.Hall) ? null : dto.Hall.Trim();
        table.Seats = dto.Seats;
        table.BranchId = dto.BranchId ?? table.BranchId;
        table.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return TableDto.FromEntity(table);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Stol topilmadi: {id}");

        var hasOpenOrder = await _db.Sales
            .AnyAsync(s => s.TableId == id && s.Status == SaleStatus.Pending, cancellationToken);
        if (hasOpenOrder)
            throw new ConflictException("Stolda ochiq buyurtma bor — o'chirib bo'lmaydi.");

        _db.RestaurantTables.Remove(table);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
