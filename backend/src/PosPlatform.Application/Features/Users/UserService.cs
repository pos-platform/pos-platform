using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Users.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Users;

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

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            BranchId = request.BranchId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(user);
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
        {
            throw new ValidationException("O'z hisobingizni faolsizlantira olmaysiz.");
        }

        var user = await GetUserAsync(id, cancellationToken);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(user);
    }

    private async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {id}");
}
