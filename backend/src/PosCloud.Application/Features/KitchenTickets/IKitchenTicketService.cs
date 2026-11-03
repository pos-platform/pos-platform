using PosCloud.Application.Features.KitchenTickets.Dtos;

namespace PosCloud.Application.Features.KitchenTickets;

public interface IKitchenTicketService
{
    /// <summary>Agent uchun: chop etilmagan eng eski oshxona cheki.</summary>
    Task<KitchenTicketDto?> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Agent uchun: oshxona cheki chop etildi deb belgilash.</summary>
    Task MarkPrintedAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
