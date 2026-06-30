using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }

    /// <summary>Foydalanuvchi telefon raqami (ixtiyoriy).</summary>
    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Hisob faolmi. Faolsiz foydalanuvchi tizimga kira olmaydi (o'chirilmaydi, faqat bloklanadi).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Foydalanuvchi biriktirilgan filial (ixtiyoriy).</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    /// <summary>Foydalanuvchi tegishli tashkilot (OrganizationId BaseEntity'da). SuperAdmin uchun null.</summary>
    public Organization? Organization { get; set; }

    /// <summary>Foydalanuvchining org darajasidagi roli. SuperAdmin uchun null.</summary>
    public Guid? AppRoleId { get; set; }

    public Role? AppRole { get; set; }

    /// <summary>
    /// Obuna shu sanagacha amal qiladi (oylik to'lov). SuperAdmin har bir kassir uchun
    /// alohida belgilaydi/uzaytiradi. null yoki o'tgan sana = faollashtirilmagan/muddati tugagan.
    /// SuperAdmin rolidagi foydalanuvchilarga taalluqli emas.
    /// </summary>
    public DateTime? LicensedUntil { get; set; }

    /// <summary>
    /// Doimiy (bir martalik to'liq sotib olingan) litsenziya. true bo'lsa — obuna hech qachon
    /// tugamaydi, oylik to'lov talab qilinmaydi va <see cref="LicensedUntil"/> e'tiborga olinmaydi.
    /// SuperAdmin har bir kassir uchun alohida belgilaydi.
    /// </summary>
    public bool IsLifetime { get; set; }
}
