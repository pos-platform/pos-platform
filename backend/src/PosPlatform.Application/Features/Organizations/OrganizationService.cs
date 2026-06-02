using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Audit;
using PosPlatform.Application.Features.Organizations.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;

    public OrganizationService(IApplicationDbContext db, IPasswordHasher passwordHasher, IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    private static DateTime Today => DateTime.UtcNow.Date;

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var today = Today;
        var orgs = await _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

        // Foydalanuvchi statistikasini bitta so'rovda yig'amiz.
        var stats = await _db.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId != null)
            .GroupBy(u => u.OrganizationId!.Value)
            .Select(g => new
            {
                OrgId = g.Key,
                Total = g.Count(),
                Licensed = g.Count(u => u.LicensedUntil != null && u.LicensedUntil.Value.Date >= today)
            })
            .ToListAsync(ct);

        return orgs.Select(o =>
        {
            var s = stats.FirstOrDefault(x => x.OrgId == o.Id);
            return OrganizationDto.FromEntity(o, s?.Total ?? 0, s?.Licensed ?? 0);
        }).ToList();
    }

    public async Task<OrganizationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var org = await GetOrgAsync(id, ct);
        var users = await _db.Users
            .AsNoTracking()
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
            Note = request.Note?.Trim(),
            IsActive = true
        };
        _db.Organizations.Add(org);

        var admin = new User
        {
            Username = adminUsername,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            Role = UserRole.Admin,
            IsActive = true,
            Organization = org,
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
        org.Note = request.Note?.Trim();
        await _db.SaveChangesAsync(ct);
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

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            OrganizationId = orgId,
            IsActive = true,
            LicensedUntil = request.LicenseMonths > 0 ? Today.AddMonths(request.LicenseMonths) : null
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("user.create", $"username={username}, role={request.Role}", organizationId: orgId, ct: ct);

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

    public async Task<OrgUserDto> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await GetManagedUserAsync(userId, ct);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(isActive ? "user.activate" : "user.deactivate", $"username={user.Username}",
            organizationId: user.OrganizationId, ct: ct);
        return OrgUserDto.FromEntity(user, Today);
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
        var total = await _db.Users.CountAsync(u => u.OrganizationId == org.Id, ct);
        var licensed = await _db.Users.CountAsync(
            u => u.OrganizationId == org.Id && u.LicensedUntil != null && u.LicensedUntil.Value.Date >= today, ct);
        return OrganizationDto.FromEntity(org, total, licensed);
    }
}
