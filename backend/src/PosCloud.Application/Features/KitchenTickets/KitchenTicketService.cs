using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.KitchenTickets.Dtos;

namespace PosCloud.Application.Features.KitchenTickets;

public class KitchenTicketService : IKitchenTicketService
{
    private readonly IApplicationDbContext _db;

    public KitchenTicketService(IApplicationDbContext db) => _db = db;

    public async Task<KitchenTicketDto?> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var ticket = await _db.KitchenTickets
            .AsNoTracking()
            .Include(t => t.Items)
            .Where(t => !t.Printed)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return ticket is null ? null : KitchenTicketDto.FromEntity(ticket);
    }

    public async Task MarkPrintedAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.KitchenTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Oshxona cheki topilmadi: {ticketId}");

        ticket.Printed = true;
        ticket.PrintedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
