using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Stores;
using PosPlatform.Application.Features.Stores.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Do'konlar (Магазины) va kassalar boshqaruvi.</summary>
[ApiController]
[Authorize]
[Route("api/stores")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _stores;

    public StoresController(IStoreService stores)
    {
        _stores = stores;
    }

    /// <summary>Do'konlar (kassalari bilan) — har qanday autentifikatsiya qilingan foydalanuvchi (filtrlar uchun).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoreDto>>> GetAll(CancellationToken cancellationToken)
    {
        var stores = await _stores.GetAllAsync(cancellationToken);
        return Ok(stores);
    }

    /// <summary>Yangi do'kon — Admin.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StoreDto>> CreateStore(CreateStoreRequest request, CancellationToken cancellationToken)
        => Ok(await _stores.CreateStoreAsync(request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StoreDto>> Activate(Guid id, CancellationToken cancellationToken)
        => Ok(await _stores.SetStoreActiveAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StoreDto>> Deactivate(Guid id, CancellationToken cancellationToken)
        => Ok(await _stores.SetStoreActiveAsync(id, false, cancellationToken));

    /// <summary>Do'konga kassa qo'shish — Admin.</summary>
    [HttpPost("cashboxes")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CashboxDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashboxDto>> AddCashbox(CreateCashboxRequest request, CancellationToken cancellationToken)
        => Ok(await _stores.AddCashboxAsync(request, cancellationToken));

    [HttpPost("cashboxes/{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CashboxDto>> ActivateCashbox(Guid id, CancellationToken cancellationToken)
        => Ok(await _stores.SetCashboxActiveAsync(id, true, cancellationToken));

    [HttpPost("cashboxes/{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CashboxDto>> DeactivateCashbox(Guid id, CancellationToken cancellationToken)
        => Ok(await _stores.SetCashboxActiveAsync(id, false, cancellationToken));

    /// <summary>Foydalanuvchini do'konga biriktirish — Admin.</summary>
    [HttpPut("{storeId:guid}/users/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignUser(Guid storeId, Guid userId, CancellationToken cancellationToken)
    {
        await _stores.AssignUserToBranchAsync(userId, storeId, cancellationToken);
        return NoContent();
    }

    /// <summary>Foydalanuvchini do'kondan uzish — Admin.</summary>
    [HttpDelete("{storeId:guid}/users/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveUser(Guid storeId, Guid userId, CancellationToken cancellationToken)
    {
        await _stores.AssignUserToBranchAsync(userId, null, cancellationToken);
        return NoContent();
    }
}
