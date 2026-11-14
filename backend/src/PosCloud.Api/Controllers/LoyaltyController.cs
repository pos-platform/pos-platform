using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.Loyalty.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Bonus (cashback) dasturi: sozlamalar, QR bonus kartani tanish, hisobot.</summary>
[ApiController]
[Authorize]
[Route("api/loyalty")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _service;

    public LoyaltyController(ILoyaltyService service) => _service = service;

    [HttpGet("settings")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<LoyaltySettingsDto>> GetSettings(CancellationToken ct) =>
        Ok(await _service.GetSettingsAsync(ct));

    /// <summary>Sozlamalarni saqlash. categoryRates — to'liq ro'yxat ({categoryId, percent}); javobda categoryName bilan.</summary>
    [HttpPut("settings")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<LoyaltySettingsDto>> UpdateSettings(UpdateLoyaltySettingsRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateSettingsAsync(request, ct));

    /// <summary>Kassada mijoz QR bonus kartasini skanerlash. Kod loglanmaydi.</summary>
    [HttpPost("resolve")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<ResolveBonusCardResponse>> Resolve(ResolveBonusCardRequest request, CancellationToken ct) =>
        Ok(await _service.ResolveAsync(request.Code, ct));

    /// <summary>Hisobot: from/to — YYYY-MM-DD, "to" kuni ham kiradi.</summary>
    [HttpGet("report")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<ActionResult<LoyaltyReportDto>> Report([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct) =>
        Ok(await _service.GetReportAsync(from, to, ct));

    /// <summary>Sozlamalar o'zgarishlari tarixi: { items: [{ changedAt, userName, field, oldValue, newValue }], total }.</summary>
    [HttpGet("settings/history")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<LoyaltyPageDto<LoyaltySettingsChangeDto>>> SettingsHistory([FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await _service.GetSettingsHistoryAsync(page, pageSize, ct));

    /// <summary>Barcha mijozlar bo'yicha bonus harakatlari jurnali (from/to — YYYY-MM-DD, "to" kuni ham kiradi).</summary>
    [HttpGet("transactions")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<LoyaltyTransactionsPageDto>> Transactions([FromQuery] LoyaltyTransactionsQuery query,
        CancellationToken ct) =>
        Ok(await _service.GetTransactionsAsync(query, ct));

    /// <summary>Qo'lda tuzatish (Adjust): { points (±), reason } — sabab majburiy.</summary>
    [HttpPost("customers/{customerId:guid}/adjust")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<LoyaltyAdjustResultDto>> Adjust(Guid customerId, LoyaltyAdjustRequest request,
        CancellationToken ct) =>
        Ok(await _service.AdjustAsync(customerId, request, ct));
}
