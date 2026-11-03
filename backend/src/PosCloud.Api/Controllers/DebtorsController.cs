using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Debtors;
using PosCloud.Application.Features.Debtors.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Qarzdorlar (Должники).</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/debtors")]
public class DebtorsController : ControllerBase
{
    private readonly IDebtorService _debtors;

    public DebtorsController(IDebtorService debtors)
    {
        _debtors = debtors;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DebtorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DebtorDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _debtors.GetAllAsync(cancellationToken));

    /// <summary>Qarzdor (harakatlar tarixi bilan).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DebtorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtorDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _debtors.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(DebtorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DebtorDto>> Create(CreateDebtorRequest request, CancellationToken cancellationToken)
        => Ok(await _debtors.CreateAsync(request, cancellationToken));

    /// <summary>Qarz qo'shish yoki to'lov qabul qilish.</summary>
    [HttpPost("{id:guid}/transactions")]
    [ProducesResponseType(typeof(DebtorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtorDto>> AddTransaction(Guid id, AddDebtorTransactionRequest request, CancellationToken cancellationToken)
        => Ok(await _debtors.AddTransactionAsync(id, request, cancellationToken));
}
