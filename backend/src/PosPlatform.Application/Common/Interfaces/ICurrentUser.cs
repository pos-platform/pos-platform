namespace PosPlatform.Application.Common.Interfaces;

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

    /// <summary>So'rov kelgan IP manzil (reverse proxy ortida X-Forwarded-For hisobga olinadi).</summary>
    string? IpAddress { get; }
}
