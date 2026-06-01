using PosPlatform.Application.Features.Shifts.Dtos;

namespace PosPlatform.Application.Features.Shifts;

public interface ICashShiftService
{
    /// <summary>Joriy kassirning ochiq smenasi (bo'lmasa null).</summary>
    Task<CashShiftDto?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<CashShiftDto> OpenAsync(OpenShiftRequest request, CancellationToken cancellationToken = default);

    Task<CashShiftDto> CloseAsync(CloseShiftRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashShiftDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CashShiftDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
