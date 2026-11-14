using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.SystemErrors;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Controllers;

/// <summary>SuperAdmin: tizim xatoliklari (API, fon, fiskal, desktop, mobil, agent).</summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/errors")]
public class AdminErrorsController : ControllerBase
{
    private readonly ISystemErrorService _service;

    public AdminErrorsController(ISystemErrorService service) => _service = service;

    /// <summary>status: open (standart) | resolved | all. from/to — YYYY-MM-DD (LastSeenAt bo'yicha).</summary>
    [HttpGet]
    public async Task<ActionResult<SystemErrorListDto>> GetList([FromQuery] SystemErrorQuery query, CancellationToken ct) =>
        Ok(await _service.GetListAsync(query, ct));

    [HttpGet("summary")]
    public async Task<ActionResult<SystemErrorSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await _service.GetSummaryAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SystemErrorDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(id, ct));

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<SystemErrorDto>> Resolve(Guid id, CancellationToken ct) =>
        Ok(await _service.ResolveAsync(id, ct));

    [HttpPost("resolve-bulk")]
    public async Task<ActionResult<ResolveErrorsResultDto>> ResolveBulk(ResolveErrorsRequest request, CancellationToken ct) =>
        Ok(await _service.ResolveBulkAsync(request.Ids, ct));
}

/// <summary>
/// Klient xatolari: desktop (xodim tokeni), mobil (mijoz tokeni yoki tokensiz), agent. Tashkilot/foydalanuvchi faqat
/// tokendan olinadi (body'dan emas). IP bo'yicha daqiqasiga 30 ta; o'lchamlar cheklangan, matn tozalanadi.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/client-errors")]
public class ClientErrorsController : ControllerBase
{
    private static readonly SystemErrorSource[] AllowedSources =
        [SystemErrorSource.Desktop, SystemErrorSource.Mobile, SystemErrorSource.Agent, SystemErrorSource.Fiscal];

    private readonly ISystemErrorRecorder _recorder;

    public ClientErrorsController(ISystemErrorRecorder recorder) => _recorder = recorder;

    [HttpPost]
    [EnableRateLimiting("client-errors")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Post(ClientErrorRequest request, CancellationToken ct)
    {
        if (!AllowedSources.Contains(request.Source))
            return BadRequest(new { status = 400, error = "source: Desktop, Mobile, Agent yoki Fiscal." });
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { status = 400, error = "message bo'sh." });

        Guid? orgId = null, branchId = null, userId = null;
        string? userName = null;

        // Xodim tokeni (desktop/agent) yoki mijoz tokeni (mobil) — ixtiyoriy, yaroqsiz bo'lsa ham xato bermaydi.
        var staff = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (staff.Succeeded)
        {
            var p = staff.Principal;
            orgId = TryGuid(p.FindFirstValue("organizationId"));
            branchId = TryGuid(p.FindFirstValue("branchId"));
            userId = TryGuid(p.FindFirstValue(ClaimTypes.NameIdentifier));
            userName = p.FindFirstValue(ClaimTypes.Name);
        }
        else if (await HttpContext.TryGetShopUserIdAsync() is { } shopUserId)
        {
            userId = shopUserId;
            userName = "Mijoz";
        }

        var details = request.Details;
        if (request.Context is { Count: > 0 } context)
        {
            var json = JsonSerializer.Serialize(context);
            if (json.Length > 2000) json = json[..2000];
            details = string.IsNullOrEmpty(details) ? $"Context: {json}" : $"{details}\n\nContext: {json}";
        }

        await _recorder.RecordAsync(new SystemErrorEntry
        {
            Source = request.Source,
            Level = Enum.IsDefined(request.Level) ? request.Level : SystemErrorLevel.Error,
            Message = request.Message,
            Details = details,
            ErrorType = "client",
            OrganizationId = orgId,
            BranchId = branchId,
            UserId = userId,
            UserName = userName,
            AppVersion = request.AppVersion,
            Device = request.Device,
            RequestId = HttpContext.TraceIdentifier
        }, ct);

        return NoContent();
    }

    private static Guid? TryGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;
}
