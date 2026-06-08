using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Users.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? AppRoleId { get; set; }
    public string? AppRoleName { get; set; }
    public bool IsActive { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LicensedUntil { get; set; }

    public static UserDto FromEntity(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role,
        AppRoleId = u.AppRoleId,
        AppRoleName = u.AppRole?.Name,
        IsActive = u.IsActive,
        BranchId = u.BranchId,
        BranchName = u.Branch?.Name,
        CreatedAt = u.CreatedAt,
        LicensedUntil = u.LicensedUntil
    };
}

public class CreateUserRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>DB darajasidagi rol ID. Berilmasa UserRole enum bo'yicha avtomatik topiladi.</summary>
    public Guid? AppRoleId { get; set; }

    public Guid? BranchId { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public Guid RoleId { get; set; }
}

public class ResetPasswordRequest
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
