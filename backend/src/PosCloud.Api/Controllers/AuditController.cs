using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Audit;
using PosCloud.Application.Features.Audit.Dtos;

namespace PosCloud.Api.Controllers;

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
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetRecent([FromQuery] AuditLogQuery query, CancellationToken ct = default)
        => Ok(await _audit.GetRecentAsync(query, ct));

    /// <summary>Fiskalizatsiya muvaffaqiyatsiz tugagan cheklar (barcha tashkilotlar bo'yicha).</summary>
    [HttpGet("fiscal-failures")]
    public async Task<ActionResult<IReadOnlyList<FiscalFailureDto>>> GetFiscalFailures([FromQuery] FiscalFailureQuery query, CancellationToken ct = default)
        => Ok(await _audit.GetFiscalFailuresAsync(query, ct));

    /// <summary>Fiskal audit filtri uchun tashkilot/kassa ro'yxatlari.</summary>
    [HttpGet("fiscal-failures/filters")]
    public async Task<ActionResult<FiscalFilterOptionsDto>> GetFiscalFilterOptions(CancellationToken ct = default)
        => Ok(await _audit.GetFiscalFilterOptionsAsync(ct));
}
