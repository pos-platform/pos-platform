using PosPlatform.Application.Features.Roles.Dtos;

namespace PosPlatform.Application.Features.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Yangi tashkilot uchun Admin/Manager/Cashier standart rollarini yaratadi.</summary>
    Task InitializeOrgRolesAsync(Guid orgId, CancellationToken ct = default);
}
