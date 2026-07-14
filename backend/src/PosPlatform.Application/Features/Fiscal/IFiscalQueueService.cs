using PosPlatform.Application.Features.Fiscal.Dtos;

namespace PosPlatform.Application.Features.Fiscal;

/// <summary>
/// Lokal fiskal agent backend bilan shu interfeys orqali ishlaydi:
/// kutayotgan cheklarni oladi va fiskalizatsiya natijasini qaytaradi.
/// </summary>
public interface IFiscalQueueService
{
    /// <summary>
    /// Fiskalizatsiya kutayotgan cheklar. <paramref name="terminalId"/> berilsa — faqat shu
    /// kassa stantsiyasining cheklari; bo'sh bo'lsa — barcha (bitta agentli eski rejim).
    /// </summary>
    Task<IReadOnlyList<PendingReceiptDto>> GetPendingAsync(int max, string? terminalId = null, CancellationToken cancellationToken = default);

    Task ApplyResultAsync(Guid saleId, FiscalResultDto result, CancellationToken cancellationToken = default);
}
