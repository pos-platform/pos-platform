using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.KitchenIssues;
using PosCloud.Application.Features.KitchenIssues.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Ombordan oshpazga berish (xom-ashyo) va oshxona sarfi hisoboti.</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/kitchen-issues")]
public class KitchenIssuesController : ControllerBase
{
    private readonly IKitchenIssueService _service;

    public KitchenIssuesController(IKitchenIssueService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KitchenIssueDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<KitchenIssueDto>> Create(CreateKitchenIssueRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    /// <summary>Ovqat retsepti asosida N porsiya uchun taklif qilingan tovarlar.</summary>
    [HttpGet("suggested")]
    public async Task<ActionResult<IReadOnlyList<SuggestedIssueItemDto>>> Suggested(
        [FromQuery] Guid dishId, [FromQuery] decimal portions, CancellationToken ct)
        => Ok(await _service.GetSuggestedItemsAsync(dishId, portions, ct));

    /// <summary>Oshxona sarfi hisoboti (ovqat bo'yicha sarflangan tovarlar).</summary>
    [HttpGet("consumption")]
    public async Task<ActionResult<IReadOnlyList<KitchenConsumptionRow>>> Consumption(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _service.GetConsumptionReportAsync(from, to, ct));
}
