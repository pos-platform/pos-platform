using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Audit;
using PosPlatform.Application.Features.Auth.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuditService _audit;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditService audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _audit = audit;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .Include(u => u.Organization)
            .Include(u => u.AppRole)
                .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync("login.fail", $"username={username}", success: false,
                actorUsername: username, ct: cancellationToken);
            throw new UnauthorizedException("Username yoki parol noto'g'ri.");
        }

        if (!user.IsActive)
        {
            await _audit.LogAsync("login.fail", "hisob bloklangan", success: false,
                organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
            throw new UnauthorizedException("Hisob faolsizlantirilgan. Administratorga murojaat qiling.");
        }

        // Obuna (litsenziya) tekshiruvi — SuperAdmin'dan tashqari barcha foydalanuvchilar uchun.
        if (user.Role != UserRole.SuperAdmin)
        {
            if (user.Organization is { IsActive: false })
            {
                await _audit.LogAsync("login.fail", "tashkilot bloklangan", success: false,
                    organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
                throw new UnauthorizedException("Tashkilot bloklangan. Platforma administratoriga murojaat qiling.");
            }

            var today = DateTime.UtcNow.Date;
            if (user.LicensedUntil is null || user.LicensedUntil.Value.Date < today)
            {
                await _audit.LogAsync("login.fail", "obuna muddati tugagan/faollashtirilmagan", success: false,
                    organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
                throw new UnauthorizedException(
                    "Obuna faollashtirilmagan yoki muddati tugagan. To'lovni amalga oshirib, administrator bilan bog'laning.");
            }
        }

        await _audit.LogAsync("login.success", $"role={user.Role}", success: true,
            organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var permissions = user.AppRole?.RolePermissions
            .Select(rp => rp.Permission.Code)
            .ToList() ?? [];

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, permissions);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            RoleName = user.AppRole?.Name,
            AppRoleId = user.AppRoleId,
            Token = token,
            ExpiresAt = expiresAt,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name,
            LicensedUntil = user.LicensedUntil,
            Permissions = permissions
        };
    }
}
