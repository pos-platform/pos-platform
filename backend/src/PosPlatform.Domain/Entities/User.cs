using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Hisob faolmi. Faolsiz foydalanuvchi tizimga kira olmaydi (o'chirilmaydi, faqat bloklanadi).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Foydalanuvchi biriktirilgan filial (ixtiyoriy).</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    /// <summary>Foydalanuvchi tegishli tashkilot. SuperAdmin uchun null.</summary>
    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    /// <summary>
    /// Obuna shu sanagacha amal qiladi (oylik to'lov). SuperAdmin har bir kassir uchun
    /// alohida belgilaydi/uzaytiradi. null yoki o'tgan sana = faollashtirilmagan/muddati tugagan.
    /// SuperAdmin rolidagi foydalanuvchilarga taalluqli emas.
    /// </summary>
    public DateTime? LicensedUntil { get; set; }
}
