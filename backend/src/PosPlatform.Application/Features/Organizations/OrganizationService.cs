using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Common.Models;
using PosPlatform.Application.Features.Audit;
using PosPlatform.Application.Features.Organizations.Dtos;
using PosPlatform.Application.Features.Roles;
using PosPlatform.Application.Features.Stores;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;
    private readonly IRoleService _roleService;

    public OrganizationService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IAuditService audit,
        IRoleService roleService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _roleService = roleService;
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    public async Task<PagedResult<OrganizationDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var today = Today;

        var totalCount = await _db.Organizations.CountAsync(ct);

        var orgs = await _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Faqat Desktop foydalanuvchilar (Kassir/Menejer) hisoblanadi — Admin panel bepul.
        var orgIds = orgs.Select(o => o.Id).ToList();
        var stats = await _db.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId != null
                        && orgIds.Contains(u.OrganizationId!.Value)
                        && u.Role != UserRole.Admin
                        && u.Role != UserRole.SuperAdmin)
            .GroupBy(u => u.OrganizationId!.Value)
            .Select(g => new
            {
                OrgId = g.Key,
                Total = g.Count(),
                Licensed = g.Count(u => u.IsLifetime || (u.LicensedUntil != null && u.LicensedUntil.Value.Date >= today))
            })
            .ToListAsync(ct);

        var items = orgs.Select(o =>
        {
            var s = stats.FirstOrDefault(x => x.OrgId == o.Id);
            return OrganizationDto.FromEntity(o, s?.Total ?? 0, s?.Licensed ?? 0);
        }).ToList();

        return new PagedResult<OrganizationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrganizationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.AppRole)
            .Include(u => u.Branch)
            .Where(u => u.OrganizationId == id)
            .ToListAsync(ct);

        return OrganizationDetailDto.FromEntity(org, users, Today);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var adminUsername = request.AdminUsername.Trim();

        if (await _db.Users.AnyAsync(u => u.Username == adminUsername, ct))
        {
            throw new ConflictException($"'{adminUsername}' username allaqachon band.");
        }

        var org = new Organization
        {
            Name = name,
            Phone = request.Phone?.Trim(),
            Stir = request.Stir?.Trim(),
            Note = request.Note?.Trim(),
            BusinessType = request.BusinessType,
            IsActive = true
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);

        // Yangi org uchun standart rollarni seed qilamiz.
        await _roleService.InitializeOrgRolesAsync(org.Id, ct);

        // Tashkilot uchun do'konlarni avtomatik yaratamiz.
        for (int i = 1; i <= request.StoreCount; i++)
        {
            _db.Branches.Add(new Branch
            {
                Name = i == 1 ? "Do'kon 1" : $"Do'kon {i}",
                BusinessType = request.BusinessType,
                IsActive = true,
                OrganizationId = org.Id
            });
        }
        await _db.SaveChangesAsync(ct);

        var adminRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.Name == "Admin", ct);

        var admin = new User
        {
            Username = adminUsername,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            Role = UserRole.Admin,
            AppRoleId = adminRole?.Id,
            IsActive = true,
            OrganizationId = org.Id,
            LicensedUntil = request.LicenseMonths > 0 ? Today.AddMonths(request.LicenseMonths) : null
        };
        _db.Users.Add(admin);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("org.create", $"name={name}, admin={adminUsername}", organizationId: org.Id, ct: ct);

        return OrganizationDto.FromEntity(org, userCount: 1, licensedCount: admin.LicensedUntil is not null ? 1 : 0);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        org.Name = request.Name.Trim();
        org.Phone = request.Phone?.Trim();
        org.Stir = request.Stir?.Trim();
        org.Note = request.Note?.Trim();
        org.BusinessType = request.BusinessType;
        await _db.SaveChangesAsync(ct);
        return await ToDtoWithStats(org, ct);
    }

    public async Task<OrganizationDto> GetSummaryAsync(Guid id, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        return await ToDtoWithStats(org, ct);
    }

    public async Task<OrganizationDto> SetBusinessTypeAsync(Guid id, BusinessType type, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        org.BusinessType = type;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("org.businessType", $"name={org.Name}, type={type}", organizationId: org.Id, ct: ct);
        return await ToDtoWithStats(org, ct);
    }

    public async Task<OrganizationDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        org.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isActive ? "org.activate" : "org.deactivate", $"name={org.Name}", organizationId: org.Id, ct: ct);
        return await ToDtoWithStats(org, ct);
    }

    public async Task<OrgUserDto> CreateUserAsync(Guid orgId, CreateOrgUserRequest request, CancellationToken ct = default)
    {
        await GetOrgAsync(orgId, ct); // tashkilot mavjudligini tekshiramiz

        if (request.Role == UserRole.SuperAdmin)
        {
            throw new ValidationException("Tashkilot ichida SuperAdmin yaratib bo'lmaydi.");
        }

        var username = request.Username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
        {
            throw new ConflictException($"'{username}' username allaqachon band.");
        }

        var roleName = request.Role.ToString();
        var appRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.Name == roleName, ct);

        var user = new User
        {
            Username = username,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            AppRoleId = appRole?.Id,
            OrganizationId = orgId,
            BranchId = request.BranchId,
            TerminalId = TerminalIdentity.Normalize(request.TerminalId),
            IsActive = true,
            LicensedUntil = request.LicenseMonths > 0 ? Today.AddMonths(request.LicenseMonths) : null
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("user.create", $"username={username}, role={request.Role}", organizationId: orgId, ct: ct);

        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> UpdateUserAsync(Guid userId, UpdateOrgUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.AppRole)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {userId}");

        if (user.Role is UserRole.SuperAdmin)
            throw new ValidationException("SuperAdmin foydalanuvchini tahrirlab bo'lmaydi.");
        if (request.Role is UserRole.SuperAdmin)
            throw new ValidationException("Tashkilot ichida SuperAdmin yaratib bo'lmaydi.");

        var username = request.Username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username && u.Id != userId, ct))
            throw new ConflictException($"'{username}' username allaqachon band.");

        // Rol o'zgargan bo'lsa — shu tashkilotning mos AppRole'ini topamiz.
        if (request.Role != user.Role)
        {
            var roleName = request.Role.ToString();
            var appRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.OrganizationId == user.OrganizationId && r.Name == roleName, ct);
            user.AppRoleId = appRole?.Id;
            user.AppRole = appRole;
        }

        user.Username = username;
        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Role = request.Role;
        user.BranchId = request.BranchId;
        user.TerminalId = TerminalIdentity.Normalize(request.TerminalId);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (request.NewPassword.Length < 6)
                throw new ValidationException("Parol kamida 6 belgidan iborat bo'lishi kerak.");
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("user.update", $"username={username}, role={request.Role}",
            organizationId: user.OrganizationId, ct: ct);

        // Branch navigatsiyasi o'zgargan bo'lishi mumkin — DTO uchun qayta yuklaymiz.
        if (user.BranchId != user.Branch?.Id)
            user.Branch = user.BranchId is null ? null
                : await _db.Branches.FirstOrDefaultAsync(b => b.Id == user.BranchId, ct);

        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> ExtendLicenseAsync(Guid userId, int months, CancellationToken ct = default)
    {
        if (months <= 0) throw new ValidationException("Oylar soni musbat bo'lishi kerak.");

        var user = await GetManagedUserAsync(userId, ct);
        // Joriy muddat hali tugamagan bo'lsa — undan davom ettiramiz, aks holda bugundan.
        var start = user.LicensedUntil is { } lu && lu.Date >= Today ? lu.Date : Today;
        user.LicensedUntil = start.AddMonths(months);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("license.extend", $"username={user.Username}, +{months} oy, until={user.LicensedUntil:yyyy-MM-dd}",
            organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> RevokeLicenseAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetManagedUserAsync(userId, ct);
        user.LicensedUntil = null;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("license.revoke", $"username={user.Username}", organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> SetLifetimeAsync(Guid userId, bool isLifetime, CancellationToken ct = default)
    {
        var user = await GetManagedUserAsync(userId, ct);
        if (user.Role is UserRole.Admin)
            throw new ValidationException("Admin uchun doimiy litsenziya qo'llanmaydi — admin panel bepul.");

        user.IsLifetime = isLifetime;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isLifetime ? "license.lifetime.set" : "license.lifetime.unset",
            $"username={user.Username}", organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> ResetDeviceAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetManagedUserAsync(userId, ct);
        var previous = user.DeviceName ?? user.DeviceId ?? "(bog'lanmagan)";

        user.DeviceId = null;
        user.DeviceName = null;
        user.DeviceBoundAt = null;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("login.device.reset", $"username={user.Username}, eski qurilma={previous}",
            organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await GetManagedUserAsync(userId, ct);

        if (isActive && user.OrganizationId is not null)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId, ct);
            if (org is not null && !org.IsActive)
                throw new ValidationException("Tashkilot bloklangan — avval tashkilotni faollashtiring.");
        }

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isActive ? "user.activate" : "user.deactivate", $"username={user.Username}",
            organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<OrgUserDto> AssignAppRoleAsync(Guid userId, Guid? roleId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.AppRole)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {userId}");

        if (user.Role == UserRole.SuperAdmin)
            throw new ValidationException("SuperAdmin rolini o'zgartirish mumkin emas.");

        if (roleId.HasValue)
        {
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == user.OrganizationId, ct)
                ?? throw new NotFoundException($"Rol topilmadi: {roleId}");
            user.AppRoleId = roleId;
            user.AppRole = role;
        }
        else
        {
            user.AppRoleId = null;
            user.AppRole = null;
        }

        await _db.SaveChangesAsync(ct);
        return OrgUserDto.FromEntity(user, Today);
    }

    public async Task<IReadOnlyList<OrgRoleDto>> GetRolesAsync(Guid orgId, CancellationToken ct = default)
    {
        await GetOrgAsync(orgId, ct);

        // Agar tashkilotda hali rol bo'lmasa (eski tashkilotlar uchun) — avtomatik yaratamiz.
        var hasRoles = await _db.Roles.AnyAsync(r => r.OrganizationId == orgId, ct);
        if (!hasRoles)
            await _roleService.InitializeOrgRolesAsync(orgId, ct);

        return await _db.Roles
            .AsNoTracking()
            .Where(r => r.OrganizationId == orgId)
            .OrderBy(r => r.Name)
            .Select(r => new OrgRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystem = r.IsSystem,
                PermissionCount = r.RolePermissions.Count
            })
            .ToListAsync(ct);
    }

    public async Task<OrgAlertsDto> GetAlertsAsync(CancellationToken ct = default)
    {
        var today = Today;
        var soon = today.AddDays(7);

        // Faqat Desktop foydalanuvchilar (Kassir/Menejer) tekshiriladi.
        // Doimiy litsenziyalilar eslatmaga tushmaydi — ularning muddati yo'q.
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .Where(u => u.OrganizationId != null
                        && u.Role != UserRole.Admin
                        && u.Role != UserRole.SuperAdmin
                        && !u.IsLifetime)
            .ToListAsync(ct);

        var orgIds = users.Select(u => u.OrganizationId!.Value).Distinct().ToList();
        var orgs = await _db.Organizations
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
            .ToListAsync(ct);
        var orgMap = orgs.ToDictionary(o => o.Id);

        OrgAlertItemDto BuildItem(Guid orgId, IEnumerable<User> orgUsers) =>
            new()
            {
                OrgId = orgId,
                OrgName = orgMap.TryGetValue(orgId, out var o) ? o.Name : orgId.ToString(),
                OrgPhone = orgMap.TryGetValue(orgId, out var o2) ? o2.Phone : null,
                Users = orgUsers.Select(u => new OrgUserAlertDto
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Role = u.Role.ToString(),
                    BranchName = u.Branch?.Name,
                    LicensedUntil = u.LicensedUntil,
                    DaysLeft = u.LicensedUntil is null ? null : (int)(u.LicensedUntil.Value.Date - today).TotalDays
                }).ToList()
            };

        var unpaid = users
            .Where(u => u.LicensedUntil is null)
            .GroupBy(u => u.OrganizationId!.Value)
            .Select(g => BuildItem(g.Key, g))
            .OrderBy(x => x.OrgName)
            .ToList();

        var expiringSoon = users
            .Where(u => u.LicensedUntil is not null
                        && u.LicensedUntil.Value.Date >= today
                        && u.LicensedUntil.Value.Date <= soon)
            .GroupBy(u => u.OrganizationId!.Value)
            .Select(g => BuildItem(g.Key, g))
            .OrderBy(x => x.OrgName)
            .ToList();

        return new OrgAlertsDto { Unpaid = unpaid, ExpiringSoon = expiringSoon };
    }

    // ---------- yordamchilar ----------

    private async Task<Organization> GetOrgAsync(Guid id, CancellationToken ct) =>
        await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new NotFoundException($"Tashkilot topilmadi: {id}");

    private async Task<User> GetManagedUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {userId}");
        if (user.Role == UserRole.SuperAdmin)
        {
            throw new ValidationException("SuperAdmin obunasini boshqarib bo'lmaydi.");
        }
        return user;
    }

    private async Task<OrganizationDto> ToDtoWithStats(Organization org, CancellationToken ct)
    {
        var today = Today;
        var total = await _db.Users.CountAsync(
            u => u.OrganizationId == org.Id && u.Role != UserRole.Admin && u.Role != UserRole.SuperAdmin, ct);
        var licensed = await _db.Users.CountAsync(
            u => u.OrganizationId == org.Id
                 && u.Role != UserRole.Admin && u.Role != UserRole.SuperAdmin
                 && (u.IsLifetime || (u.LicensedUntil != null && u.LicensedUntil.Value.Date >= today)), ct);
        return OrganizationDto.FromEntity(org, total, licensed);
    }
}
