using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Payments;
using PosCloud.Application.Features.Payments.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>To'lovlar (Платежи) — yetkazib beruvchiga to'lov.</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ISupplierPaymentService _payments;

    public PaymentsController(ISupplierPaymentService payments)
    {
        _payments = payments;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPaymentDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _payments.GetAllAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(SupplierPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPaymentDto>> Create(CreateSupplierPaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _payments.CreateAsync(request, cancellationToken));
}
