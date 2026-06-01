using PosPlatform.Application.Features.Expenses.Dtos;

namespace PosPlatform.Application.Features.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
