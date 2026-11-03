using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Inventories;

namespace PosCloud.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/inventories")]
public class InventoriesController : ControllerBase
{
    private readonly IInventoryService _inventories;

    public InventoriesController(IInventoryService inventories) => _inventories = inventories;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _inventories.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _inventories.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryRequest req, CancellationToken ct) =>
        Ok(await _inventories.CreateAsync(req, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateActualQty(Guid id, [FromBody] UpdateInventoryRequest req, CancellationToken ct) =>
        Ok(await _inventories.UpdateActualQtyAsync(id, req, ct));

    [HttpPost("{id:guid}/apply")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Apply(Guid id, CancellationToken ct) =>
        Ok(await _inventories.ApplyAsync(id, ct));
}
