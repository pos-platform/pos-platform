using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Dashboard;
using PosCloud.Application.Features.Dashboard.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Bosh sahifa (Главный) KPI hisoblari.</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Davr va do'kon bo'yicha umumiy ko'rsatkichlar.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        var summary = await _dashboard.GetSummaryAsync(from, to, branchId, cancellationToken);
        return Ok(summary);
    }
}
