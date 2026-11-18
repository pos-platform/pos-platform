using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Stock;
using PosCloud.Application.Features.Stock.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Tovar kirimi (Накладная).</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/stock-receipts")]
public class StockReceiptsController : ControllerBase
{
    private readonly IStockReceiptService _receipts;

    public StockReceiptsController(IStockReceiptService receipts)
    {
        _receipts = receipts;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockReceiptDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockReceiptDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _receipts.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StockReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockReceiptDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _receipts.GetByIdAsync(id, cancellationToken));

    /// <summary>Kirim yaratadi (ombor + tannarx + yetkazib beruvchi balansi yangilanadi).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockReceiptDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockReceiptDto>> Create(CreateStockReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await _receipts.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
    }

    /// <summary>Kirimni bekor qiladi (storno). Partiyadan sotilgan bo'lsa — 400.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(StockReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockReceiptDto>> Cancel(Guid id, CancellationToken cancellationToken)
        => Ok(await _receipts.CancelAsync(id, cancellationToken));
}
