using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Tables;
using PosPlatform.Application.Features.Tables.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Restoran stollari va zallari.</summary>
[ApiController]
[Authorize]
[Route("api/tables")]
public class TablesController : ControllerBase
{
    private readonly ITableService _tables;

    public TablesController(ITableService tables) => _tables = tables;

    /// <summary>Stollar (ochiq buyurtma holati bilan). branchId bilan filtrlanadi.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TableDto>>> GetAll([FromQuery] Guid? branchId, CancellationToken ct)
        => Ok(await _tables.GetAllAsync(branchId, ct));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TableDto>> Create(CreateTableDto dto, CancellationToken ct)
        => Ok(await _tables.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TableDto>> Update(Guid id, UpdateTableDto dto, CancellationToken ct)
        => Ok(await _tables.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tables.DeleteAsync(id, ct);
        return NoContent();
    }
}
