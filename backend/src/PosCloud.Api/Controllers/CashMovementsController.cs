using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.CashMovements;
using PosCloud.Application.Features.CashMovements.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Kassa harakatlari: naqd kirim, chiqim, inkassatsiya.</summary>
[ApiController]
[Authorize]
[Route("api/cash-movements")]
public class CashMovementsController : ControllerBase
{
    private readonly ICashMovementService _service;

    public CashMovementsController(ICashMovementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashMovementDto>>> GetAll(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? shiftId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(branchId, shiftId, cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.CashMovements)]
    public async Task<ActionResult<CashMovementDto>> Create(CreateCashMovementRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateAsync(request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.CashMovements)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
