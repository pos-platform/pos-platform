using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.Promotions.Dtos;

namespace PosCloud.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/promotions")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotions;

    public PromotionsController(IPromotionService promotions)
    {
        _promotions = promotions;
    }

    /// <summary>Hozir amal qilayotgan aksiyalar (POS uchun) — har qanday foydalanuvchi.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> GetActive(CancellationToken cancellationToken)
    {
        var promos = await _promotions.GetActiveAsync(cancellationToken);
        return Ok(promos);
    }

    /// <summary>Barcha aksiyalar — Admin.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var promos = await _promotions.GetAllAsync(cancellationToken);
        return Ok(promos);
    }

    /// <summary>Yangi aksiya — Admin.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionDto>> Create(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var promo = await _promotions.CreateAsync(request, cancellationToken);
        return Ok(promo);
    }

    /// <summary>Aksiyani yoqish — Admin.</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PromotionDto>> Activate(Guid id, CancellationToken cancellationToken)
        => Ok(await _promotions.SetActiveAsync(id, true, cancellationToken));

    /// <summary>Aksiyani o'chirish (vaqtincha) — Admin.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PromotionDto>> Deactivate(Guid id, CancellationToken cancellationToken)
        => Ok(await _promotions.SetActiveAsync(id, false, cancellationToken));

    /// <summary>Aksiyani butunlay o'chirish — Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _promotions.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
