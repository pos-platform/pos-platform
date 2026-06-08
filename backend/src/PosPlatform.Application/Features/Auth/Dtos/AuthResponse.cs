using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Auth.Dtos;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>DB darajasidagi rol nomi (Admin/Manager/Cashier yoki custom).</summary>
    public string? RoleName { get; set; }

    public Guid? AppRoleId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public Guid? OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    public DateTime? LicensedUntil { get; set; }

    public List<string> Permissions { get; set; } = [];
}
