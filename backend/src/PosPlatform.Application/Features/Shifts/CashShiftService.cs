using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Shifts.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Shifts;

public class CashShiftService : ICashShiftService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CashShiftService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid CurrentCashierId =>
        _currentUser.UserId ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

    public async Task<CashShiftDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var shift = await FindOpenShiftAsync(CurrentCashierId, cancellationToken);
        return shift is null ? null : CashShiftDto.FromEntity(shift);
    }

    public async Task<CashShiftDto> OpenAsync(OpenShiftRequest request, CancellationToken cancellationToken = default)
    {
        var cashierId = CurrentCashierId;

        var existing = await FindOpenShiftAsync(cashierId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Sizda allaqachon ochiq smena bor. Avval uni yoping.");
        }

        var shift = new CashShift
        {
            CashierId = cashierId,
            BranchId = request.BranchId ?? _currentUser.BranchId,
            OpeningCash = request.OpeningCash,
            Status = ShiftStatus.Open,
            OpenedAt = DateTime.UtcNow
        };

        _db.CashShifts.Add(shift);
        await _db.SaveChangesAsync(cancellationToken);

        return CashShiftDto.FromEntity(shift);
    }

    public async Task<CashShiftDto> CloseAsync(CloseShiftRequest request, CancellationToken cancellationToken = default)
    {
        var shift = await FindOpenShiftAsync(CurrentCashierId, cancellationToken)
            ?? throw new NotFoundException("Ochiq smena topilmadi.");

        var sales = await _db.Sales
            .Where(s => s.CashShiftId == shift.Id)
            .ToListAsync(cancellationToken);

        decimal SumWhere(Func<Sale, bool> predicate) =>
            sales.Where(predicate).Sum(s => s.TotalAmount);

        shift.TotalSales = SumWhere(s => s.Type == SaleType.Sale);
        shift.TotalRefunds = SumWhere(s => s.Type == SaleType.Refund);

        shift.CashSalesNet =
            SumWhere(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Cash)
            - SumWhere(s => s.Type == SaleType.Refund && s.PaymentMethod == PaymentMethod.Cash);

        shift.CardSalesNet =
            SumWhere(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Card)
            - SumWhere(s => s.Type == SaleType.Refund && s.PaymentMethod == PaymentMethod.Card);

        shift.ExpectedCash = shift.OpeningCash + shift.CashSalesNet;
        shift.CountedCash = request.CountedCash;
        shift.CashDifference = request.CountedCash - shift.ExpectedCash;
        shift.ClosedAt = DateTime.UtcNow;
        shift.Status = ShiftStatus.Closed;

        await _db.SaveChangesAsync(cancellationToken);

        return CashShiftDto.FromEntity(shift);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await _db.CashShifts
            .AsNoTracking()
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync(cancellationToken);

        return shifts.Select(CashShiftDto.FromEntity).ToList();
    }

    public async Task<CashShiftDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await _db.CashShifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shift is null)
        {
            throw new NotFoundException($"Smena topilmadi: {id}");
        }

        return CashShiftDto.FromEntity(shift);
    }

    private async Task<CashShift?> FindOpenShiftAsync(Guid cashierId, CancellationToken cancellationToken) =>
        await _db.CashShifts
            .FirstOrDefaultAsync(
                s => s.CashierId == cashierId && s.Status == ShiftStatus.Open,
                cancellationToken);
}
