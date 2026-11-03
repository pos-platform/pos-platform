using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Features.Fiscal;
using PosCloud.Application.Features.Fiscal.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>
/// Lokal fiskal agent (FDS / Rahmat MK) uchun endpointlar. API-kalit bilan himoyalangan.
/// Agent kutayotgan cheklarni "tortib oladi" va fiskalizatsiya natijasini qaytaradi.
/// </summary>
[ApiController]
[AgentApiKey]
[Route("api/fiscal")]
public class FiscalController : ControllerBase
{
    private readonly IFiscalQueueService _fiscalQueue;

    public FiscalController(IFiscalQueueService fiscalQueue)
    {
        _fiscalQueue = fiscalQueue;
    }

    /// <summary>
    /// Fiskalizatsiya kutayotgan cheklar (Queued). <paramref name="terminalId"/> berilsa —
    /// faqat shu kassa stantsiyasining cheklari (har terminalda o'z fiskal moduli/agenti bo'lganda).
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingReceiptDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingReceiptDto>>> GetPending(
        [FromQuery] int max = 20,
        [FromQuery] string? terminalId = null,
        CancellationToken cancellationToken = default)
    {
        var pending = await _fiscalQueue.GetPendingAsync(max, terminalId, cancellationToken);
        return Ok(pending);
    }

    /// <summary>Agent fiskalizatsiya natijasini qaytaradi.</summary>
    [HttpPost("{saleId:guid}/result")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostResult(
        Guid saleId,
        FiscalResultDto result,
        CancellationToken cancellationToken)
    {
        await _fiscalQueue.ApplyResultAsync(saleId, result, cancellationToken);
        return NoContent();
    }
}
