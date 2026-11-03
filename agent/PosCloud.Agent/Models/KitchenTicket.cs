namespace PosCloud.Agent.Models;

public class KitchenTicketDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<KitchenTicketLine> Items { get; set; } = new();
}

public class KitchenTicketLine
{
    public string DishName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}
