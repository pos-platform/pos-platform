using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.KitchenTickets.Dtos;

public class KitchenTicketDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<KitchenTicketLineDto> Items { get; set; } = new();

    public static KitchenTicketDto FromEntity(KitchenTicket t) => new()
    {
        Id = t.Id,
        SaleId = t.SaleId,
        TableName = t.TableName,
        WaiterName = t.WaiterName,
        CreatedAt = t.CreatedAt,
        Items = t.Items.Select(KitchenTicketLineDto.FromEntity).ToList()
    };
}

public class KitchenTicketLineDto
{
    public string DishName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Note { get; set; }

    public static KitchenTicketLineDto FromEntity(KitchenTicketItem i) => new()
    {
        DishName = i.DishName,
        Quantity = i.Quantity,
        Note = i.Note
    };
}
