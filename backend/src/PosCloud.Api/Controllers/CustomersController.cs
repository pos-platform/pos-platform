using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Customers;
using PosCloud.Application.Features.Customers.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Mijozlar va sodiqlik dasturi.</summary>
[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _customers.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _customers.GetByIdAsync(id, cancellationToken));

    [HttpGet("by-phone")]
    public async Task<ActionResult<CustomerDto?>> ByPhone([FromQuery] string phone, CancellationToken cancellationToken)
        => Ok(await _customers.FindByPhoneAsync(phone, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/adjust-points")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<CustomerDto>> AdjustPoints(Guid id, AdjustPointsRequest request, CancellationToken cancellationToken)
        => Ok(await _customers.AdjustPointsAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _customers.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
