using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.CashMovements.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.CashMovements;

public class CashMovementService : ICashMovementService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CashMovementService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CashMovementDto>> GetAllAsync(
        Guid? branchId = null,
        Guid? shiftId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CashMovements.AsNoTracking().AsQueryable();

        if (branchId.HasValue)
            query = query.Where(m => m.BranchId == branchId);
        if (shiftId.HasValue)
            query = query.Where(m => m.CashShiftId == shiftId);

        var list = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(CashMovementDto.FromEntity).ToList();
    }

    public async Task<CashMovementDto> CreateAsync(CreateCashMovementRequest request, CancellationToken cancellationToken = default)
    {
        var cashierId = _currentUser.UserId;

        // Joriy ochiq smenani topamiz (ixtiyoriy).
        Guid? shiftId = request.CashShiftId;
        if (shiftId is null && cashierId.HasValue)
        {
            var shift = await _db.CashShifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.Status == ShiftStatus.Open, cancellationToken);
            shiftId = shift?.Id;
        }

        var movement = new CashMovement
        {
            Type = request.Type,
            Amount = request.Amount,
            Comment = request.Comment?.Trim(),
            BranchId = request.BranchId,
            CashShiftId = shiftId
        };

        _db.CashMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        return CashMovementDto.FromEntity(movement);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var movement = await _db.CashMovements
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kassa harakati topilmadi: {id}");

        _db.CashMovements.Remove(movement);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
