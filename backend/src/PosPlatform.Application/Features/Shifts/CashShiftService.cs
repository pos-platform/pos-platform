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
        var cashierId = CurrentCashierId;
        var shift = await FindOpenShiftAsync(cashierId, cancellationToken)
            ?? throw new NotFoundException("Ochiq smena topilmadi.");

        var sales = await _db.Sales
            .Include(s => s.Items)
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

        shift.TransferSalesNet =
            SumWhere(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Transfer)
            - SumWhere(s => s.Type == SaleType.Refund && s.PaymentMethod == PaymentMethod.Transfer);

        // Kassa harakatlari (kirim − chiqim − inkassatsiya).
        var movements = await _db.CashMovements
            .Where(m => m.CashShiftId == shift.Id)
            .ToListAsync(cancellationToken);

        decimal movementsNet = 0m;
        foreach (var m in movements)
        {
            movementsNet += m.Type switch
            {
                Domain.Enums.CashMovementType.Income => m.Amount,
                Domain.Enums.CashMovementType.Expense => -m.Amount,
                Domain.Enums.CashMovementType.Incassation => -m.Amount,
                _ => 0
            };
        }

        shift.ExpectedCash = shift.OpeningCash + shift.CashSalesNet + movementsNet;
        shift.CountedCash = request.CountedCash;
        shift.CashDifference = request.CountedCash - shift.ExpectedCash;
        shift.ClosedAt = DateTime.UtcNow;
        shift.Status = ShiftStatus.Closed;

        await _db.SaveChangesAsync(cancellationToken);

        var cashier = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == cashierId, cancellationToken);

        return CashShiftDto.FromEntity(shift, sales, cashier?.Username);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await _db.CashShifts
            .AsNoTracking()
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync(cancellationToken);

        return shifts.Select(s => CashShiftDto.FromEntity(s)).ToList();
    }

    public async Task<CashShiftDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await _db.CashShifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Smena topilmadi: {id}");

        var sales = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Where(s => s.CashShiftId == shift.Id)
            .ToListAsync(cancellationToken);

        var cashier = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == shift.CashierId, cancellationToken);

        return CashShiftDto.FromEntity(shift, sales, cashier?.Username);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetPendingReportsAsync(int max, CancellationToken cancellationToken = default)
    {
        var shifts = await _db.CashShifts
            .AsNoTracking()
            .Where(s => s.Status == ShiftStatus.Closed && !s.ShiftReportPrinted)
            .OrderBy(s => s.ClosedAt)
            .Take(max)
            .ToListAsync(cancellationToken);

        var result = new List<CashShiftDto>();
        foreach (var shift in shifts)
        {
            var sales = await _db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => s.CashShiftId == shift.Id)
                .ToListAsync(cancellationToken);

            var cashier = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == shift.CashierId, cancellationToken);

            result.Add(CashShiftDto.FromEntity(shift, sales, cashier?.Username));
        }

        return result;
    }

    public async Task MarkReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Smena topilmadi: {shiftId}");

        shift.ShiftReportPrinted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestXReportAsync(CancellationToken cancellationToken = default)
    {
        var shift = await FindOpenShiftAsync(CurrentCashierId, cancellationToken)
            ?? throw new NotFoundException("Ochiq smena topilmadi.");

        shift.XReportRequested = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashShiftDto>> GetPendingXReportsAsync(int max, CancellationToken cancellationToken = default)
    {
        var shifts = await _db.CashShifts
            .AsNoTracking()
            .Where(s => s.Status == ShiftStatus.Open && s.XReportRequested)
            .OrderBy(s => s.OpenedAt)
            .Take(max)
            .ToListAsync(cancellationToken);

        var result = new List<CashShiftDto>();
        foreach (var shift in shifts)
        {
            var sales = await _db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => s.CashShiftId == shift.Id)
                .ToListAsync(cancellationToken);

            var cashier = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == shift.CashierId, cancellationToken);

            result.Add(CashShiftDto.FromEntity(shift, sales, cashier?.Username));
        }

        return result;
    }

    public async Task MarkXReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Smena topilmadi: {shiftId}");

        shift.XReportRequested = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CashShift?> FindOpenShiftAsync(Guid cashierId, CancellationToken cancellationToken) =>
        await _db.CashShifts
            .FirstOrDefaultAsync(
                s => s.CashierId == cashierId && s.Status == ShiftStatus.Open,
                cancellationToken);
}
