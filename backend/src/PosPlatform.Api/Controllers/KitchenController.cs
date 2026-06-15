using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Api.Authentication;
using PosPlatform.Application.Features.KitchenTickets;
using PosPlatform.Application.Features.KitchenTickets.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Oshxona cheki navbati — lokal agent polling orqali chop etadi.</summary>
[ApiController]
[Route("api/kitchen")]
public class KitchenController : ControllerBase
{
    private readonly IKitchenTicketService _tickets;

    public KitchenController(IKitchenTicketService tickets) => _tickets = tickets;

    /// <summary>Agent: keyingi chop etilmagan oshxona cheki.</summary>
    [HttpGet("pending")]
    [AllowAnonymous]
    [AgentApiKey]
    public async Task<ActionResult<KitchenTicketDto?>> Pending(CancellationToken ct)
    {
        var ticket = await _tickets.GetPendingAsync(ct);
        return ticket is null ? NoContent() : Ok(ticket);
    }

    /// <summary>Agent: oshxona cheki chop etildi deb belgilash.</summary>
    [HttpPost("{id:guid}/printed")]
    [AllowAnonymous]
    [AgentApiKey]
    public async Task<IActionResult> Printed(Guid id, CancellationToken ct)
    {
        await _tickets.MarkPrintedAsync(id, ct);
        return NoContent();
    }
}
