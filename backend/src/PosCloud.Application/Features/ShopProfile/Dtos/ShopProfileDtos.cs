using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopProfile.Dtos;

public class ShopProfileDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Email { get; set; }
    public int AddressCount { get; set; }
    public bool ProfileCompleted { get; set; }
}

public class UpdateShopProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public string? Email { get; set; }
}

public class ShopAddressDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public string? Comment { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class SaveShopAddressRequest
{
    /// <summary>Uy, Ish yoki mijoz yozgan nom.</summary>
    [Required]
    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(20)] public string? Entrance { get; set; }
    [MaxLength(20)] public string? Floor { get; set; }
    [MaxLength(20)] public string? Apartment { get; set; }
    [MaxLength(50)] public string? Intercom { get; set; }
    [MaxLength(200)] public string? Landmark { get; set; }
    [MaxLength(500)] public string? Comment { get; set; }

    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }

    public bool IsDefault { get; set; }
}
