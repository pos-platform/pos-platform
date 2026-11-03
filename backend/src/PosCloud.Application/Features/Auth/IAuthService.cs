using PosCloud.Application.Features.Auth.Dtos;

namespace PosCloud.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Joriy (kirgan) foydalanuvchi o'z parolini o'zgartiradi — joriy parolni tasdiqlab.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
