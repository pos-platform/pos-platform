using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Audit;
using PosPlatform.Application.Features.Audit.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Xavfsizlik/audit jurnali — faqat platforma egasi (SuperAdmin) uchun.</summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetRecent([FromQuery] int take = 200, CancellationToken ct = default)
        => Ok(await _audit.GetRecentAsync(take, ct));
}
