using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Expenses;
using PosCloud.Application.Features.Expenses.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Xarajatlar (расходы).</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public ExpensesController(IExpenseService expenses)
    {
        _expenses = expenses;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _expenses.GetAllAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseRequest request, CancellationToken cancellationToken)
        => Ok(await _expenses.CreateAsync(request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _expenses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
