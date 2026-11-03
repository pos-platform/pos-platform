using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Features.Labels;
using PosCloud.Application.Features.Labels.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Narx yorliq chop etish.</summary>
[ApiController]
[Route("api/labels")]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labels;

    public LabelsController(ILabelService labels) => _labels = labels;

    /// <summary>Yangi chop etish vazifasi yaratish (POS / admin tomonidan).</summary>
    [HttpPost("print")]
    [Authorize]
    public async Task<ActionResult<LabelPrintJobDto>> Print(CreateLabelJobRequest request, CancellationToken cancellationToken)
        => Ok(await _labels.CreateJobAsync(request, cancellationToken));

    /// <summary>Agent: keyingi chop etilmagan vazifani olish.</summary>
    [HttpGet("pending")]
    [AllowAnonymous]
    [AgentApiKey]
    public async Task<ActionResult<LabelPrintJobDto?>> Pending(CancellationToken cancellationToken)
    {
        var job = await _labels.GetPendingJobAsync(cancellationToken);
        return job is null ? NoContent() : Ok(job);
    }

    /// <summary>Agent: vazifa chop etildi deb belgilash.</summary>
    [HttpPost("{id:guid}/printed")]
    [AllowAnonymous]
    [AgentApiKey]
    public async Task<IActionResult> Printed(Guid id, CancellationToken cancellationToken)
    {
        await _labels.MarkPrintedAsync(id, cancellationToken);
        return NoContent();
    }
}
