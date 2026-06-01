using PosPlatform.Application.Features.Users.Dtos;

namespace PosPlatform.Application.Features.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hisobni faollashtirish/faolsizlantirish.</summary>
    Task<UserDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
