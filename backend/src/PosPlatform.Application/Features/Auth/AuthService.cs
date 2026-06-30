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
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditService audit,
        ICurrentUser currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .Include(u => u.Organization)
            .Include(u => u.Branch)
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

        // Tashkilot bloki — SuperAdmin bundan mustasno.
        if (user.Role != UserRole.SuperAdmin && user.Organization is { IsActive: false })
        {
            await _audit.LogAsync("login.fail", "tashkilot bloklangan", success: false,
                organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
            throw new UnauthorizedException("Tashkilot bloklangan. Platforma administratoriga murojaat qiling.");
        }

        // Obuna (litsenziya) tekshiruvi — faqat Kassir va Menejer uchun. Admin panel bepul.
        // Doimiy (IsLifetime) litsenziyali foydalanuvchilar tekshiruvdan o'tkazilmaydi.
        if ((user.Role == UserRole.Cashier || user.Role == UserRole.Manager) && !user.IsLifetime)
        {
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

        // Rejim filial darajasida: foydalanuvchi filialiga biriktirilgan bo'lsa — o'sha filial turi;
        // aks holda (admin/menejer) — tashkilotda kamida bitta restoran filiali bo'lsa Restaurant.
        BusinessType businessType;
        if (user.Branch is not null)
            businessType = user.Branch.BusinessType;
        else if (user.OrganizationId is { } orgId)
            businessType = await _db.Branches.AnyAsync(
                b => b.OrganizationId == orgId && b.BusinessType == BusinessType.Restaurant, cancellationToken)
                ? BusinessType.Restaurant : BusinessType.Store;
        else
            businessType = BusinessType.Store;

        return BuildResponse(user, businessType);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
            throw new UnauthorizedException("Avval tizimga kiring.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new UnauthorizedException("Foydalanuvchi topilmadi.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            await _audit.LogAsync("password.change.fail", "joriy parol noto'g'ri", success: false,
                organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
            throw new ValidationException("Joriy parol noto'g'ri.");
        }

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationException("Yangi parol joriy paroldan farq qilishi kerak.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("password.change", "parol o'zgartirildi", success: true,
            organizationId: user.OrganizationId, actorUserId: user.Id, actorUsername: user.Username, ct: cancellationToken);
    }

    private AuthResponse BuildResponse(User user, BusinessType businessType)
    {
        var permissions = user.AppRole?.RolePermissions
            .Select(rp => rp.Permission.Code)
            .ToList() ?? [];

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, permissions);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            RoleName = user.AppRole?.Name,
            AppRoleId = user.AppRoleId,
            Token = token,
            ExpiresAt = expiresAt,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name,
            BranchName = user.Branch?.Name,
            BusinessType = businessType,
            LicensedUntil = user.LicensedUntil,
            IsLifetime = user.IsLifetime,
            Permissions = permissions
        };
    }
}
