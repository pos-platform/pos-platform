using PosCloud.Application.Features.Users.Dtos;

namespace PosCloud.Application.Features.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hisobni faollashtirish/faolsizlantirish.</summary>
    Task<UserDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Foydalanuvchiga rol belgilaydi.</summary>
    Task<UserDto> AssignRoleAsync(Guid id, AssignRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Joriy tashkilotning e'tibor talab qiluvchi obunalari (org admin eslatmasi uchun).</summary>
    Task<LicenseSummaryDto> GetLicenseSummaryAsync(CancellationToken cancellationToken = default);
}
