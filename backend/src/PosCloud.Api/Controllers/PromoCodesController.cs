using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.PromoCodes;

namespace PosCloud.Api.Controllers;

/// <summary>Online buyurtma promokodlari (do'kon admini).</summary>
[ApiController]
[Authorize]
[Route("api/promo-codes")]
[HasPermission(Permissions.SettingsManage)]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _service;

    public PromoCodesController(IPromoCodeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromoCodeDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<PromoCodeDto>> Create(SavePromoCodeRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PromoCodeDto>> Update(Guid id, SavePromoCodeRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
