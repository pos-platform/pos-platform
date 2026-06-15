using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

/// <summary>Restoran stoli. Zal (Hall) ichida joylashadi; ochiq buyurtma bo'lsa band.</summary>
public class RestaurantTable : BaseEntity
{
    /// <summary>Stol nomi/raqami (masalan "12", "VIP-3").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Zal nomi (masalan "1-zal", "Terrasa"). v1 — oddiy matn.</summary>
    public string? Hall { get; set; }

    /// <summary>O'rinlar soni.</summary>
    public int Seats { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public TableStatus Status { get; set; } = TableStatus.Free;

    public bool IsActive { get; set; } = true;
}
