namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Joriy autentifikatsiya qilingan foydalanuvchi (JWT'dan). Implementatsiya
/// Api qatlamida HttpContext orqali beriladi.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
    Guid? BranchId { get; }
    Guid? OrganizationId { get; }

    /// <summary>
    /// Foydalanuvchiga biriktirilgan kassa stantsiyasi ID (JWT "terminalId" claim).
    /// null = kassa biriktirilmagan, cheklov yo'q.
    /// </summary>
    string? TerminalId { get; }

    /// <summary>So'rov kelgan IP manzil (reverse proxy ortida X-Forwarded-For hisobga olinadi).</summary>
    string? IpAddress { get; }

    /// <summary>JWT tokendan o'qilgan ruxsatlar (permission code'lari). SuperAdmin uchun bo'sh.</summary>
    IReadOnlyList<string> Permissions { get; }
}
