using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

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

    /// <summary>Soliq to'lovchi identifikatsiya raqami (STIR / ИНН) — fiskal chek sarlavhasida chiqadi.</summary>
    public string? Stir { get; set; }

    public string? Note { get; set; }

    /// <summary>Faoliyat turi — Do'kon yoki Restoran. Restoran rejimida stol/oshxona/ovqat modullari yoqiladi.</summary>
    public BusinessType BusinessType { get; set; } = BusinessType.Store;

    /// <summary>Tashkilot bloklanmaganmi. Bloklansa — ichidagi barcha foydalanuvchilar kira olmaydi.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
