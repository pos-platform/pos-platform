using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Users.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher, ICurrentUser currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .Include(u => u.AppRole)
            .Include(u => u.Branch)
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(UserDto.FromEntity).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        var exists = await _db.Users.AnyAsync(u => u.Username == username, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"'{username}' username allaqachon band.");
        }

        // AppRoleId berilmagan bo'lsa, UserRole enum bo'yicha org ichidan standart rolni topamiz.
        var appRoleId = request.AppRoleId;
        if (appRoleId is null && _currentUser.OrganizationId is not null)
        {
            var roleName = request.Role.ToString();
            var matchedRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
            appRoleId = matchedRole?.Id;
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            AppRoleId = appRoleId,
            BranchId = request.BranchId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(await GetUserWithRoleAsync(user.Id, cancellationToken));
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(id, cancellationToken);
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && _currentUser.UserId == id)
            throw new ValidationException("O'z hisobingizni faolsizlantira olmaysiz.");

        var user = await GetUserAsync(id, cancellationToken);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(await GetUserWithRoleAsync(id, cancellationToken));
    }

    public async Task<UserDto> AssignRoleAsync(Guid id, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(id, cancellationToken);

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
            throw new NotFoundException($"Rol topilmadi: {request.RoleId}");

        user.AppRoleId = request.RoleId;
        await _db.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(await GetUserWithRoleAsync(id, cancellationToken));
    }

    public async Task<LicenseSummaryDto> GetLicenseSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var soon = today.AddDays(7);

        // Faqat shu tashkilotning Desktop foydalanuvchilari (Kassir/Menejer).
        // Doimiy litsenziyalilar va faolsizlar eslatmaga tushmaydi.
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .Where(u => u.IsActive
                        && !u.IsLifetime
                        && (u.Role == Domain.Enums.UserRole.Cashier || u.Role == Domain.Enums.UserRole.Manager))
            .ToListAsync(cancellationToken);

        var needsAttention = users
            .Select(u =>
            {
                string? status = u.LicensedUntil is null
                    ? "unpaid"
                    : u.LicensedUntil.Value.Date < today
                        ? "expired"
                        : u.LicensedUntil.Value.Date <= soon
                            ? "expiring"
                            : null; // muddati hali uzoq — eslatma kerak emas
                if (status is null) return null;
                return new LicenseAlertUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    BranchName = u.Branch?.Name,
                    LicensedUntil = u.LicensedUntil,
                    DaysLeft = u.LicensedUntil is null ? null : (int)(u.LicensedUntil.Value.Date - today).TotalDays,
                    Status = status
                };
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.DaysLeft ?? int.MinValue)
            .ToList();

        return new LicenseSummaryDto { NeedsAttention = needsAttention };
    }

    private async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {id}");

    private async Task<User> GetUserWithRoleAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Users
            .Include(u => u.AppRole)
            .Include(u => u.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {id}");
}
