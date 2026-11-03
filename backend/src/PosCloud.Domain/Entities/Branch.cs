using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Do'kon/filial (Магазин). Bir filialda bir nechta kassa bo'lishi mumkin.</summary>
public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    /// <summary>Filial faoliyat turi — Do'kon yoki Restoran. Bitta tashkilotda aralash bo'lishi mumkin.</summary>
    public BusinessType BusinessType { get; set; } = BusinessType.Store;

    /// <summary>Do'kon balansi (kassa pullari yig'indisi/joriy holat).</summary>
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Do'kon tegishli tashkilot (OrganizationId BaseEntity'da).</summary>
    public Organization? Organization { get; set; }

    public ICollection<Cashbox> Cashboxes { get; set; } = new List<Cashbox>();

    public ICollection<User> Users { get; set; } = new List<User>();
}
