using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.Stock;
using PosCloud.Application.Features.Stock.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Ombor harakati jurnali: tovar aylanmasi hisoboti va tovar qoldig'i tarixi.</summary>
[ApiController]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _movements;

    public StockMovementsController(IStockMovementService movements) => _movements = movements;

    /// <summary>Tovar aylanmasi: boshlang'ich qoldiq, kirim/chiqim (tur bo'yicha), oxirgi qoldiq.</summary>
    [HttpGet("api/reports/stock-turnover")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(typeof(StockTurnoverReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockTurnoverReportDto>> GetTurnover([FromQuery] StockTurnoverQuery query, CancellationToken ct)
        => Ok(await _movements.GetTurnoverAsync(query, ct));

    /// <summary>Bitta tovar qoldig'i tarixi (eng yangisi birinchi, ko'pi bilan 1000 ta).</summary>
    [HttpGet("api/products/{id:guid}/movements")]
    [HasPermission(Permissions.InventoryView)]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> GetProductMovements(Guid id,
        [FromQuery] Guid? branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _movements.GetProductMovementsAsync(id, branchId, from, to, ct));
}
