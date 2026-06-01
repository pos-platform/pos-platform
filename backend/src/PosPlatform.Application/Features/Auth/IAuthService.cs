using PosPlatform.Application.Features.Auth.Dtos;

namespace PosPlatform.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
