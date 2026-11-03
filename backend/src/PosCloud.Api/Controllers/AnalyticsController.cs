using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Analytics;
using PosCloud.Application.Features.Analytics.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Аналитика — sotuv, foyda, top tovarlar.</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsDto>> Get(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
        => Ok(await _analytics.GetAsync(from, to, branchId, cancellationToken));
}
