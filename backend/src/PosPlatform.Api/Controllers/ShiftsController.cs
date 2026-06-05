using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Shifts;
using PosPlatform.Application.Features.Shifts.Dtos;

namespace PosPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/shifts")]
public class ShiftsController : ControllerBase
{
    private readonly ICashShiftService _shifts;

    public ShiftsController(ICashShiftService shifts)
    {
        _shifts = shifts;
    }

    /// <summary>Joriy kassirning ochiq smenasi (bo'lmasa 204).</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CashShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CashShiftDto>> Current(CancellationToken cancellationToken)
    {
        var shift = await _shifts.GetCurrentAsync(cancellationToken);
        return shift is null ? NoContent() : Ok(shift);
    }

    /// <summary>Smena ochish.</summary>
    [HttpPost("open")]
    [ProducesResponseType(typeof(CashShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashShiftDto>> Open(OpenShiftRequest request, CancellationToken cancellationToken)
    {
        var shift = await _shifts.OpenAsync(request, cancellationToken);
        return Ok(shift);
    }

    /// <summary>Smenani yopish (naqd sanab, hisobot qaytaradi).</summary>
    [HttpPost("close")]
    [ProducesResponseType(typeof(CashShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashShiftDto>> Close(CloseShiftRequest request, CancellationToken cancellationToken)
    {
        var shift = await _shifts.CloseAsync(request, cancellationToken);
        return Ok(shift);
    }

    /// <summary>Smenalar tarixi.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CashShiftDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashShiftDto>>> GetAll(CancellationToken cancellationToken)
    {
        var shifts = await _shifts.GetAllAsync(cancellationToken);
        return Ok(shifts);
    }

    /// <summary>Id bo'yicha smena (hisobot bilan).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CashShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashShiftDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shift = await _shifts.GetByIdAsync(id, cancellationToken);
        return Ok(shift);
    }

    /// <summary>Agent uchun: X/Z hisobot kutayotgan yopilgan smenalar.</summary>
    [HttpGet("pending-report")]
    [ProducesResponseType(typeof(IReadOnlyList<CashShiftDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashShiftDto>>> PendingReport(
        [FromQuery] int max = 10,
        CancellationToken cancellationToken = default)
    {
        var shifts = await _shifts.GetPendingReportsAsync(max, cancellationToken);
        return Ok(shifts);
    }

    /// <summary>Agent uchun: X/Z hisobot chop etildi deb belgilash.</summary>
    [HttpPost("{id:guid}/report-printed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportPrinted(Guid id, CancellationToken cancellationToken)
    {
        await _shifts.MarkReportPrintedAsync(id, cancellationToken);
        return NoContent();
    }
}
