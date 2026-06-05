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
}
