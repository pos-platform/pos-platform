using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Expenses.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IApplicationDbContext _db;

    public ExpenseService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var expenses = await _db.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return expenses.Select(ExpenseDto.FromEntity).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = new Expense
        {
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Comment = request.Comment?.Trim(),
            BranchId = request.BranchId
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        return ExpenseDto.FromEntity(expense);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Xarajat topilmadi: {id}");

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
