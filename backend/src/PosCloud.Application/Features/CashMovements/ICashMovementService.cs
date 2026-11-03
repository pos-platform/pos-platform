using PosCloud.Application.Features.CashMovements.Dtos;

namespace PosCloud.Application.Features.CashMovements;

public interface ICashMovementService
{
    Task<IReadOnlyList<CashMovementDto>> GetAllAsync(Guid? branchId = null, Guid? shiftId = null, CancellationToken cancellationToken = default);
    Task<CashMovementDto> CreateAsync(CreateCashMovementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
