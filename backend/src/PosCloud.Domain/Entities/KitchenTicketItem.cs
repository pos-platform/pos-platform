using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>Oshxona cheki qatori — ovqat nomi + miqdor + izoh (narxsiz snapshot).</summary>
public class KitchenTicketItem : BaseEntity
{
    public Guid KitchenTicketId { get; set; }
    public KitchenTicket? KitchenTicket { get; set; }

    public string DishName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}
