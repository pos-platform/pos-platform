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
}
