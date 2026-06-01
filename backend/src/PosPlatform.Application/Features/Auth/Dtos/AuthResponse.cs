using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Auth.Dtos;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
