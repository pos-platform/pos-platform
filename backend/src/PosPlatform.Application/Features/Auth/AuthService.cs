using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Auth.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Username yoki parol noto'g'ri.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Hisob faolsizlantirilgan. Administratorga murojaat qiling.");
        }

        // Obuna (litsenziya) tekshiruvi — SuperAdmin'dan tashqari barcha foydalanuvchilar uchun.
        if (user.Role != UserRole.SuperAdmin)
        {
            if (user.Organization is { IsActive: false })
            {
                throw new UnauthorizedException("Tashkilot bloklangan. Platforma administratoriga murojaat qiling.");
            }

            var today = DateTime.UtcNow.Date;
            if (user.LicensedUntil is null || user.LicensedUntil.Value.Date < today)
            {
                throw new UnauthorizedException(
                    "Obuna faollashtirilmagan yoki muddati tugagan. To'lovni amalga oshirib, administrator bilan bog'laning.");
            }
        }

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name,
            LicensedUntil = user.LicensedUntil
        };
    }
}
