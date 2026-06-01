using PosPlatform.Application.Features.Fiscal.Dtos;

namespace PosPlatform.Application.Features.Fiscal;

/// <summary>
/// Lokal fiskal agent backend bilan shu interfeys orqali ishlaydi:
/// kutayotgan cheklarni oladi va fiskalizatsiya natijasini qaytaradi.
/// </summary>
public interface IFiscalQueueService
{
    Task<IReadOnlyList<PendingReceiptDto>> GetPendingAsync(int max, CancellationToken cancellationToken = default);

    Task ApplyResultAsync(Guid saleId, FiscalResultDto result, CancellationToken cancellationToken = default);
}
