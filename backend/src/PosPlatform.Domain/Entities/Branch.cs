using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>Do'kon (Магазин). Bir do'konda bir nechta kassa bo'lishi mumkin.</summary>
public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    /// <summary>Do'kon balansi (kassa pullari yig'indisi/joriy holat).</summary>
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Do'kon tegishli tashkilot (ixtiyoriy — eski ma'lumotlar uchun null bo'lishi mumkin).</summary>
    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public ICollection<Cashbox> Cashboxes { get; set; } = new List<Cashbox>();

    public ICollection<User> Users { get; set; } = new List<User>();
}
