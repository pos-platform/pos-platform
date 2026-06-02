using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Tashkilot (mijoz). Platforma egasi (SuperAdmin) tomonidan yaratiladi.
/// Bir tashkilotga bir nechta do'kon (Branch) va kassir (User) tegishli bo'ladi.
/// Obuna har bir kassirga alohida — SuperAdmin har bir foydalanuvchini alohida
/// faollashtiradi (LicensedUntil sanasini belgilab).
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Note { get; set; }

    /// <summary>Tashkilot bloklanmaganmi. Bloklansa — ichidagi barcha foydalanuvchilar kira olmaydi.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
