using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Sales;
using PosPlatform.Application.Features.Sales.Dtos;

namespace PosPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    /// <summary>Barcha savdolar (eng yangisi birinchi). branchId bilan filtrlanadi.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SaleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleDto>>> GetAll(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        var sales = await _saleService.GetAllAsync(branchId, cancellationToken);
        return Ok(sales);
    }

    /// <summary>Id bo'yicha savdo (qatorlar va chek bilan).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _saleService.GetByIdAsync(id, cancellationToken);
        return Ok(sale);
    }

    /// <summary>Yangi savdo yaratadi (qatorlarni hisoblaydi, chek raqami beradi, fiskal navbatga qo'yadi).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> Create(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var sale = await _saleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    /// <summary>Savdoni qaytaradi (refund) — fiskal return cheki yaratiladi.</summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleDto>> Refund(Guid id, CancellationToken cancellationToken)
    {
        var refund = await _saleService.RefundAsync(id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = refund.Id }, refund);
    }

    // ===================== Restoran: ochiq buyurtmalar =====================

    /// <summary>Stolda yangi ochiq buyurtma ochadi (yoki mavjudini qaytaradi).</summary>
    [HttpPost("orders/open")]
    public async Task<ActionResult<SaleDto>> OpenOrder(OpenOrderRequest request, CancellationToken ct)
        => Ok(await _saleService.OpenOrderAsync(request, ct));

    /// <summary>Ochiq (Pending) buyurtmalar ro'yxati.</summary>
    [HttpGet("orders/open")]
    public async Task<ActionResult<IReadOnlyList<SaleDto>>> GetOpenOrders([FromQuery] Guid? branchId, CancellationToken ct)
        => Ok(await _saleService.GetOpenOrdersAsync(branchId, ct));

    /// <summary>Stolning ochiq buyurtmasi (bo'lsa).</summary>
    [HttpGet("orders/by-table/{tableId:guid}")]
    public async Task<ActionResult<SaleDto>> GetOpenOrderByTable(Guid tableId, CancellationToken ct)
    {
        var order = await _saleService.GetOpenOrderByTableAsync(tableId, ct);
        return order is null ? NoContent() : Ok(order);
    }

    /// <summary>Ochiq buyurtma qatorlarini saqlaydi.</summary>
    [HttpPut("orders/{id:guid}/items")]
    public async Task<ActionResult<SaleDto>> SaveOrderItems(Guid id, SaveOrderItemsRequest request, CancellationToken ct)
        => Ok(await _saleService.SaveOrderItemsAsync(id, request, ct));

    /// <summary>Yangi qatorlarni oshxonaga yuboradi.</summary>
    [HttpPost("orders/{id:guid}/send-to-kitchen")]
    public async Task<ActionResult<object>> SendToKitchen(Guid id, CancellationToken ct)
    {
        var count = await _saleService.SendToKitchenAsync(id, ct);
        return Ok(new { sent = count });
    }

    /// <summary>Ochiq buyurtmani to'laydi (yopadi) — fiskal chek yaratiladi.</summary>
    [HttpPost("orders/{id:guid}/pay")]
    public async Task<ActionResult<SaleDto>> PayOrder(Guid id, PayOrderRequest request, CancellationToken ct)
        => Ok(await _saleService.PayOrderAsync(id, request, ct));

    /// <summary>Ochiq buyurtmani bekor qiladi.</summary>
    [HttpPost("orders/{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        await _saleService.CancelOrderAsync(id, ct);
        return NoContent();
    }
}
