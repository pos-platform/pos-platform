using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Suppliers;
using PosCloud.Application.Features.Suppliers.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Yetkazib beruvchilar (Поставщики).</summary>
[ApiController]
[Authorize]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers)
    {
        _suppliers = suppliers;
    }

    /// <summary>Barcha yetkazib beruvchilar (tovar formasi ham ishlatadi).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _suppliers.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
        => Ok(await _suppliers.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
        => Ok(await _suppliers.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SupplierDto>> Activate(Guid id, CancellationToken cancellationToken)
        => Ok(await _suppliers.SetActiveAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SupplierDto>> Deactivate(Guid id, CancellationToken cancellationToken)
        => Ok(await _suppliers.SetActiveAsync(id, false, cancellationToken));
}
