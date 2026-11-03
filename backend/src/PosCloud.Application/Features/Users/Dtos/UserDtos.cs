using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Users.Dtos;

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

    /// <summary>Doimiy (bir martalik to'liq sotib olingan) litsenziya.</summary>
    public bool IsLifetime { get; set; }

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
        LicensedUntil = u.LicensedUntil,
        IsLifetime = u.IsLifetime
    };
}

/// <summary>Org admin uchun obuna eslatmasi — bitta kassir/menejerning to'lov holati.</summary>
public class LicenseAlertUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? BranchName { get; set; }
    public DateTime? LicensedUntil { get; set; }
    public int? DaysLeft { get; set; }

    /// <summary>"expiring" (≤7 kun qoldi), "expired" (tugagan) yoki "unpaid" (hech qachon to'lanmagan).</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Org admin uchun e'tibor talab qiluvchi obunalar ro'yxati (to'lovga undash uchun).</summary>
public class LicenseSummaryDto
{
    public List<LicenseAlertUserDto> NeedsAttention { get; set; } = new();
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
