using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Dishes;
using PosPlatform.Application.Features.Dishes.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Ovqatlar (restoran menyusi) va retsept (texnologik karta).</summary>
[ApiController]
[Authorize]
[Route("api/dishes")]
public class DishesController : ControllerBase
{
    private readonly IDishService _dishes;

    public DishesController(IDishService dishes) => _dishes = dishes;

    /// <summary>Barcha ovqatlar + retsepti.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DishDto>>> GetAll(CancellationToken ct)
        => Ok(await _dishes.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DishDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _dishes.GetByIdAsync(id, ct));

    /// <summary>Ovqat retseptini to'liq belgilash (eski qatorlar o'chadi).</summary>
    [HttpPut("{id:guid}/recipe")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<DishDto>> SetRecipe(Guid id, SetRecipeRequest request, CancellationToken ct)
        => Ok(await _dishes.SetRecipeAsync(id, request, ct));
}
