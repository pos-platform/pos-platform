using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Yetkazib beruvchi (Поставщик). Tovar kirimi (накладная) shu yetkazib beruvchidan
/// keladi; balans — unga qarzimiz (kirimda oshadi, to'lovda kamayadi).
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? ContactPerson { get; set; }

    public string? Address { get; set; }

    /// <summary>Yetkazib beruvchiga qarz (balans).</summary>
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;
}
