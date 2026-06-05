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

    /// <summary>Agent uchun: X/Z hisobot chop etilmagan yopilgan smenalar.</summary>
    Task<IReadOnlyList<CashShiftDto>> GetPendingReportsAsync(int max, CancellationToken cancellationToken = default);

    /// <summary>Agent uchun: smena hisoboti muvaffaqiyatli chop etildi deb belgilash.</summary>
    Task MarkReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>Ochiq smenada X-hisobot chop etishni so'rash.</summary>
    Task RequestXReportAsync(CancellationToken cancellationToken = default);

    /// <summary>Agent uchun: X-hisobot kutayotgan ochiq smenalar.</summary>
    Task<IReadOnlyList<CashShiftDto>> GetPendingXReportsAsync(int max, CancellationToken cancellationToken = default);

    /// <summary>Agent uchun: X-hisobot chop etildi deb belgilash.</summary>
    Task MarkXReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken = default);
}
