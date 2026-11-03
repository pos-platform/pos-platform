using PosCloud.Application.Features.Expenses.Dtos;

namespace PosCloud.Application.Features.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
