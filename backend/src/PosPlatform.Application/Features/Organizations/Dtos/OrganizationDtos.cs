using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Organizations.Dtos;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Tashkilotdagi jami foydalanuvchilar (SuperAdmin'dan tashqari).</summary>
    public int UserCount { get; set; }

    /// <summary>Hozir obunasi amalda bo'lgan (aktiv) foydalanuvchilar soni.</summary>
    public int LicensedCount { get; set; }

    public static OrganizationDto FromEntity(Organization o, int userCount, int licensedCount) => new()
    {
        Id = o.Id,
        Name = o.Name,
        Phone = o.Phone,
        Note = o.Note,
        IsActive = o.IsActive,
        CreatedAt = o.CreatedAt,
        UserCount = userCount,
        LicensedCount = licensedCount
    };
}

public class OrgUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LicensedUntil { get; set; }
    public bool IsLicensed { get; set; }
    public int? DaysLeft { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? AppRoleId { get; set; }
    public string? AppRoleName { get; set; }

    public static OrgUserDto FromEntity(User u, DateTime today)
    {
        var licensed = u.LicensedUntil is not null && u.LicensedUntil.Value.Date >= today;
        return new OrgUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role,
            IsActive = u.IsActive,
            LicensedUntil = u.LicensedUntil,
            IsLicensed = licensed,
            DaysLeft = u.LicensedUntil is null ? null : (int)(u.LicensedUntil.Value.Date - today).TotalDays,
            CreatedAt = u.CreatedAt,
            AppRoleId = u.AppRoleId,
            AppRoleName = u.AppRole?.Name
        };
    }
}

public class AssignOrgUserRoleRequest
{
    public Guid? RoleId { get; set; }
}

public class OrganizationDetailDto : OrganizationDto
{
    public List<OrgUserDto> Users { get; set; } = new();

    public static OrganizationDetailDto FromEntity(Organization o, List<User> users, DateTime today)
    {
        var licensedCount = users.Count(u => u.LicensedUntil is not null && u.LicensedUntil.Value.Date >= today);
        return new OrganizationDetailDto
        {
            Id = o.Id,
            Name = o.Name,
            Phone = o.Phone,
            Note = o.Note,
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt,
            UserCount = users.Count,
            LicensedCount = licensedCount,
            Users = users.OrderBy(u => u.Role).ThenBy(u => u.Username).Select(u => OrgUserDto.FromEntity(u, today)).ToList()
        };
    }
}

public class CreateOrganizationRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Note { get; set; }

    /// <summary>Tashkilotning bosh admin foydalanuvchisi (do'kon egasi).</summary>
    [Required]
    [MinLength(3)]
    public string AdminUsername { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Admin uchun boshlang'ich obuna (oy). 0 = faollashtirilmagan.</summary>
    public int LicenseMonths { get; set; } = 1;
}

public class CreateOrgUserRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Boshlang'ich obuna (oy). 0 = faollashtirilmagan (keyin alohida aktivlashtiriladi).</summary>
    public int LicenseMonths { get; set; } = 0;
}

/// <summary>Foydalanuvchi obunasini uzaytirish (oylik to'lov qabul qilingach).</summary>
public class ExtendLicenseRequest
{
    /// <summary>Necha oyga uzaytirish (musbat). Joriy muddat yoki bugundan kattaroqdan boshlab qo'shiladi.</summary>
    [Range(1, 60)]
    public int Months { get; set; } = 1;
}

public class UpdateOrganizationRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Note { get; set; }
}

public class OrgRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public int PermissionCount { get; set; }
}
