using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.OnlinePayments;
using PosPlatform.Application.Features.OnlinePayments.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Onlayn to'lovlar (Payme / Click).</summary>
[ApiController]
[Route("api/online-payments")]
public class OnlinePaymentsController : ControllerBase
{
    private readonly IOnlinePaymentService _service;

    public OnlinePaymentsController(IOnlinePaymentService service) => _service = service;

    /// <summary>Yangi to'lov havolasi yaratish (POS dan chaqiriladi).</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<OnlinePaymentDto>> Create(CreateOnlinePaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<OnlinePaymentDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpGet("by-sale/{saleId:guid}")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<OnlinePaymentDto>>> GetBySale(Guid saleId, CancellationToken cancellationToken)
        => Ok(await _service.GetBySaleAsync(saleId, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<ActionResult<OnlinePaymentDto>> Cancel(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.CancelAsync(id, cancellationToken));

    // --- Payme webhook (Basic Auth header yoki IP whitelist bilan himoyalanadi) ---
    [HttpPost("payme")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymeWebhook([FromBody] PaymeRpcRequest rpc, CancellationToken cancellationToken)
    {
        // Payme Merchant API Basic Auth: "Payme:<SecretKey>"
        // TODO: validate Authorization header in middleware
        var result = await _service.HandlePaymeRpcAsync(rpc, cancellationToken);
        return Ok(result);
    }

    // --- Click webhooks ---
    [HttpPost("click/prepare")]
    [AllowAnonymous]
    public async Task<ActionResult<ClickPrepareResponse>> ClickPrepare([FromForm] ClickPrepareRequest request, CancellationToken cancellationToken)
        => Ok(await _service.HandleClickPrepareAsync(request, cancellationToken));

    [HttpPost("click/complete")]
    [AllowAnonymous]
    public async Task<ActionResult<ClickCompleteResponse>> ClickComplete([FromForm] ClickCompleteRequest request, CancellationToken cancellationToken)
        => Ok(await _service.HandleClickCompleteAsync(request, cancellationToken));
}
