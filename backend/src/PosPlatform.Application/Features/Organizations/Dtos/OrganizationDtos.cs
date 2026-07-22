using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Organizations.Dtos;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Stir { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public BusinessType BusinessType { get; set; }
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
        Stir = o.Stir,
        Note = o.Note,
        IsActive = o.IsActive,
        BusinessType = o.BusinessType,
        CreatedAt = o.CreatedAt,
        UserCount = userCount,
        LicensedCount = licensedCount
    };
}

public class OrgUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LicensedUntil { get; set; }
    public bool IsLicensed { get; set; }

    /// <summary>Doimiy (bir martalik to'liq sotib olingan) litsenziya — hech qachon tugamaydi.</summary>
    public bool IsLifetime { get; set; }
    public int? DaysLeft { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? AppRoleId { get; set; }
    public string? AppRoleName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }

    /// <summary>Biriktirilgan kassa stantsiyasi ID ("KASSA-1") — bo'sh bo'lsa cheklov yo'q.</summary>
    public string? TerminalId { get; set; }

    /// <summary>Login bog'langan kompyuter nomi (bo'sh = hali bog'lanmagan).</summary>
    public string? DeviceName { get; set; }

    /// <summary>Qurilma bog'langanmi — "Qurilmani ajratish" tugmasini ko'rsatish uchun.</summary>
    public bool IsDeviceBound { get; set; }

    /// <summary>Qurilma bog'langan vaqt (UTC).</summary>
    public DateTime? DeviceBoundAt { get; set; }

    public static OrgUserDto FromEntity(User u, DateTime today)
    {
        var licensed = u.IsLifetime || (u.LicensedUntil is not null && u.LicensedUntil.Value.Date >= today);
        return new OrgUserDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Phone = u.Phone,
            Role = u.Role,
            IsActive = u.IsActive,
            LicensedUntil = u.LicensedUntil,
            IsLicensed = licensed,
            IsLifetime = u.IsLifetime,
            DaysLeft = u.IsLifetime || u.LicensedUntil is null ? null : (int)(u.LicensedUntil.Value.Date - today).TotalDays,
            CreatedAt = u.CreatedAt,
            AppRoleId = u.AppRoleId,
            AppRoleName = u.AppRole?.Name,
            BranchId = u.BranchId,
            BranchName = u.Branch?.Name,
            TerminalId = u.TerminalId,
            DeviceName = u.DeviceName,
            IsDeviceBound = !string.IsNullOrWhiteSpace(u.DeviceId),
            DeviceBoundAt = u.DeviceBoundAt
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
        // Faqat Desktop foydalanuvchilar (Kassir/Menejer) hisoblanadi.
        var desktop = users.Where(u => u.Role != UserRole.Admin && u.Role != UserRole.SuperAdmin).ToList();
        return new OrganizationDetailDto
        {
            Id = o.Id,
            Name = o.Name,
            Phone = o.Phone,
            Stir = o.Stir,
            Note = o.Note,
            IsActive = o.IsActive,
            BusinessType = o.BusinessType,
            CreatedAt = o.CreatedAt,
            UserCount = desktop.Count,
            LicensedCount = desktop.Count(u => u.IsLifetime || (u.LicensedUntil is not null && u.LicensedUntil.Value.Date >= today)),
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

    /// <summary>Soliq to'lovchi identifikatsiya raqami (STIR / ИНН) — fiskal chekda chiqadi.</summary>
    public string? Stir { get; set; }

    public string? Note { get; set; }

    /// <summary>Faoliyat turi — Do'kon (0) yoki Restoran (1).</summary>
    public BusinessType BusinessType { get; set; } = BusinessType.Store;

    /// <summary>Tashkilotning bosh admin foydalanuvchisi (do'kon egasi).</summary>
    [Required]
    [MinLength(3)]
    public string AdminUsername { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Admin uchun boshlang'ich obuna (oy). 0 = faollashtirilmagan.</summary>
    public int LicenseMonths { get; set; } = 1;

    /// <summary>Tashkilot uchun avtomatik yaratiladigan do'konlar soni (1–10).</summary>
    [Range(1, 10)]
    public int StoreCount { get; set; } = 1;
}

public class CreateOrgUserRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }

    /// <summary>Foydalanuvchi telefon raqami (ixtiyoriy).</summary>
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Boshlang'ich obuna (oy). 0 = faollashtirilmagan (keyin alohida aktivlashtiriladi).</summary>
    public int LicenseMonths { get; set; } = 0;

    /// <summary>Kassir/menejer qaysi do'konga tegishli (ixtiyoriy).</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Biriktirilgan kassa stantsiyasi ID (masalan "KASSA-1") — kassa ilovasi Sozlamalaridagi
    /// qiymat bilan bir xil bo'lishi shart. Bo'sh = istalgan kassadan kirishi mumkin.
    /// </summary>
    [MaxLength(64)]
    public string? TerminalId { get; set; }
}

/// <summary>Mavjud desktop foydalanuvchini (kassir/menejer) tahrirlash.</summary>
public class UpdateOrgUserRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }

    /// <summary>Foydalanuvchi telefon raqami (ixtiyoriy).</summary>
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Cashier;

    /// <summary>Kassir/menejer qaysi do'konga tegishli (ixtiyoriy).</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Biriktirilgan kassa stantsiyasi ID (masalan "KASSA-1") — kassa ilovasi Sozlamalaridagi
    /// qiymat bilan bir xil bo'lishi shart. Bo'sh = istalgan kassadan kirishi mumkin.
    /// </summary>
    [MaxLength(64)]
    public string? TerminalId { get; set; }

    /// <summary>Yangi parol (ixtiyoriy). Bo'sh bo'lsa parol o'zgarmaydi; berilsa kamida 6 belgi.</summary>
    public string? NewPassword { get; set; }
}

/// <summary>Foydalanuvchi obunasini uzaytirish (oylik to'lov qabul qilingach).</summary>
public class ExtendLicenseRequest
{
    /// <summary>Necha oyga uzaytirish (musbat). Joriy muddat yoki bugundan kattaroqdan boshlab qo'shiladi.</summary>
    [Range(1, 60)]
    public int Months { get; set; } = 1;
}

public class OrgUserAlertDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public DateTime? LicensedUntil { get; set; }
    public int? DaysLeft { get; set; }
}

public class OrgAlertItemDto
{
    public Guid OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? OrgPhone { get; set; }
    public List<OrgUserAlertDto> Users { get; set; } = new();
}

public class OrgAlertsDto
{
    /// <summary>Hech qachon to'lov qilinmagan kassirlar bor tashkilotlar.</summary>
    public List<OrgAlertItemDto> Unpaid { get; set; } = new();
    /// <summary>7 kun ichida obunasi tugayotgan kassirlar bor tashkilotlar.</summary>
    public List<OrgAlertItemDto> ExpiringSoon { get; set; } = new();
}

public class UpdateOrganizationRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>Soliq to'lovchi identifikatsiya raqami (STIR / ИНН) — fiskal chekda chiqadi.</summary>
    public string? Stir { get; set; }

    public string? Note { get; set; }
    public BusinessType BusinessType { get; set; } = BusinessType.Store;
}

public class OrgRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public int PermissionCount { get; set; }
}
