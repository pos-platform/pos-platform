using Microsoft.AspNetCore.Mvc;
using PosPlatform.Api.Authentication;
using PosPlatform.Application.Features.Fiscal;
using PosPlatform.Application.Features.Fiscal.Dtos;

namespace PosPlatform.Api.Controllers;

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

    /// <summary>Fiskalizatsiya kutayotgan cheklar (Queued).</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingReceiptDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingReceiptDto>>> GetPending(
        [FromQuery] int max = 20,
        CancellationToken cancellationToken = default)
    {
        var pending = await _fiscalQueue.GetPendingAsync(max, cancellationToken);
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
