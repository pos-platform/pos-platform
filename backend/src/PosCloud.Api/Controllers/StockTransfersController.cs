using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Transfers;
using PosCloud.Application.Features.Transfers.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Filiallararo tovar ko'chirish (transfer).</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/stock-transfers")]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferService _transfers;

    public StockTransfersController(IStockTransferService transfers) => _transfers = transfers;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransferDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _transfers.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockTransferDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _transfers.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Create(CreateStockTransferRequest request, CancellationToken cancellationToken)
        => Ok(await _transfers.CreateAsync(request, cancellationToken));

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<StockTransferDto>> Receive(Guid id, CancellationToken cancellationToken)
        => Ok(await _transfers.ReceiveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<StockTransferDto>> Cancel(Guid id, CancellationToken cancellationToken)
        => Ok(await _transfers.CancelAsync(id, cancellationToken));
}
