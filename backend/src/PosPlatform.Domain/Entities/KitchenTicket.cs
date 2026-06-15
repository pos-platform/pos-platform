using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Oshxona cheki (print vazifasi) — ofitsiant buyurtmani "oshxonaga yuborganda"
/// yaratiladi. Agent polling orqali oladi, oshxona printeriga chop etadi va
/// Printed=true qiladi. (LabelPrintJob andozasi.)
/// </summary>
public class KitchenTicket : BaseEntity
{
    public Guid? BranchId { get; set; }

    /// <summary>Buyurtma (Pending Sale).</summary>
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    /// <summary>Chop etishda ko'rsatish uchun snapshot.</summary>
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }

    public bool Printed { get; set; }
    public DateTime? PrintedAt { get; set; }

    public ICollection<KitchenTicketItem> Items { get; set; } = new List<KitchenTicketItem>();
}
