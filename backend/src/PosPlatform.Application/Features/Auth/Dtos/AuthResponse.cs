using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Auth.Dtos;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }

    public UserRole Role { get; set; }

    /// <summary>DB darajasidagi rol nomi (Admin/Manager/Cashier yoki custom).</summary>
    public string? RoleName { get; set; }

    public Guid? AppRoleId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public Guid? OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    /// <summary>Tashkilot STIR (ИНН) — fiskal chek sarlavhasida chiqadi.</summary>
    public string? OrganizationStir { get; set; }

    /// <summary>Foydalanuvchi biriktirilgan do'kon (filial) nomi — chek sarlavhasida ishlatiladi.</summary>
    public string? BranchName { get; set; }

    /// <summary>Filial manzili — fiskal chek sarlavhasida chiqadi.</summary>
    public string? BranchAddress { get; set; }

    /// <summary>Tashkilot faoliyat turi — Store yoki Restaurant. Frontend/desktop UI'ni shunga moslaydi.</summary>
    public BusinessType BusinessType { get; set; }

    public DateTime? LicensedUntil { get; set; }

    /// <summary>Doimiy (bir martalik to'liq sotib olingan) litsenziya — obuna hech qachon tugamaydi.</summary>
    public bool IsLifetime { get; set; }

    /// <summary>
    /// Foydalanuvchiga biriktirilgan kassa stantsiyasi ID ("KASSA-1"). null = cheklov yo'q.
    /// Desktop uni ko'rsatish va o'z sozlamasini tekshirish uchun ishlatadi.
    /// </summary>
    public string? TerminalId { get; set; }

    public List<string> Permissions { get; set; } = [];
}
