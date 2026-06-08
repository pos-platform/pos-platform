using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Authorization;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Roles.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Roles;

public class RoleService : IRoleService
{
    private readonly IApplicationDbContext _db;

    public RoleService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return roles.Select(RoleDto.FromEntity).ToList();
    }

    public async Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var role = await LoadRoleAsync(id, ct);
        return RoleDto.FromEntity(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.Roles.AnyAsync(r => r.Name == name, ct))
            throw new ConflictException($"'{name}' nomli rol allaqachon mavjud.");

        var permissions = await ResolvePermissionsAsync(request.Permissions, ct);

        var role = new Role
        {
            Name = name,
            IsSystem = false,
            RolePermissions = permissions.Select(p => new RolePermission { PermissionId = p.Id }).ToList()
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(role.Id, ct);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Rol topilmadi: {id}");

        var name = request.Name.Trim();
        if (await _db.Roles.AnyAsync(r => r.Name == name && r.Id != id, ct))
            throw new ConflictException($"'{name}' nomli rol allaqachon mavjud.");

        role.Name = name;

        var permissions = await ResolvePermissionsAsync(request.Permissions, ct);
        role.RolePermissions.Clear();
        foreach (var p in permissions)
            role.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = p.Id });

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Rol topilmadi: {id}");

        if (role.IsSystem)
            throw new ValidationException("Tizim rollari o'chirib bo'lmaydi.");

        if (await _db.Users.AnyAsync(u => u.AppRoleId == id, ct))
            throw new ConflictException("Bu rolga biriktirilgan foydalanuvchilar bor. Avval ularni boshqa rolga o'tkazing.");

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Role> LoadRoleAsync(Guid id, CancellationToken ct) =>
        await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new NotFoundException($"Rol topilmadi: {id}");

    public async Task InitializeOrgRolesAsync(Guid orgId, CancellationToken ct = default)
    {
        var permMap = await _db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        var roleDefinitions = new (string Name, string[] PermCodes)[]
        {
            ("Admin",   Permissions.AdminPermissions),
            ("Manager", Permissions.ManagerPermissions),
            ("Cashier", Permissions.CashierPermissions),
        };

        foreach (var (roleName, permCodes) in roleDefinitions)
        {
            var exists = await _db.Roles.AnyAsync(r => r.OrganizationId == orgId && r.Name == roleName, ct);
            if (exists) continue;

            var role = new Role
            {
                Name = roleName,
                IsSystem = true,
                OrganizationId = orgId,
                RolePermissions = permCodes
                    .Where(c => permMap.ContainsKey(c))
                    .Select(c => new RolePermission { PermissionId = permMap[c] })
                    .ToList()
            };
            _db.Roles.Add(role);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<Permission>> ResolvePermissionsAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var codeList = codes.Distinct().ToList();
        if (codeList.Count == 0) return [];

        var found = await _db.Permissions
            .Where(p => codeList.Contains(p.Code))
            .ToListAsync(ct);

        var unknown = codeList.Except(found.Select(p => p.Code)).ToList();
        if (unknown.Count > 0)
            throw new ValidationException($"Noma'lum ruxsatlar: {string.Join(", ", unknown)}");

        return found;
    }
}
