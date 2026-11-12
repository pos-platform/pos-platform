using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Mobil ilova foydalanuvchisi (mijoz akkaunti). GLOBAL — hech bir tashkilotga tegishli emas
/// (OrganizationId = null). Do'kondagi <see cref="Customer"/> yozuvi ShopUserId orqali bog'lanadi.
/// </summary>
public class ShopUser : BaseEntity
{
    /// <summary>Telefon, +998XXXXXXXXX ko'rinishida. Yagona.</summary>
    public string Phone { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Email { get; set; }

    public DateTime? PhoneVerifiedAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Akkaunt o'chirilgan vaqt (shaxsiy ma'lumotlar tozalanadi, buyurtmalar tarixi qoladi).</summary>
    public DateTime? DeletedAt { get; set; }

    public ICollection<ShopUserAddress> Addresses { get; set; } = new List<ShopUserAddress>();
}

/// <summary>Mijozning saqlangan manzili.</summary>
public class ShopUserAddress : BaseEntity
{
    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    /// <summary>Nomi: Uy, Ish va h.k.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Ko'cha, uy (xaritadan yoki qo'lda).</summary>
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

/// <summary>Kirish uchun bir martalik kod (faqat xeshi saqlanadi).</summary>
public class ShopOtpCode : BaseEntity
{
    /// <summary>+998XXXXXXXXX</summary>
    public string Phone { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Noto'g'ri kiritishlar soni. Limitdan oshsa kod yaroqsiz.</summary>
    public int Attempts { get; set; }

    public DateTime? ConsumedAt { get; set; }

    /// <summary>Qaysi kanal orqali yuborildi: telegram, sms, log.</summary>
    public string Channel { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
}

/// <summary>Mijoz refresh tokeni (faqat xeshi saqlanadi).</summary>
public class ShopRefreshToken : BaseEntity
{
    public Guid ShopUserId { get; set; }
    public ShopUser? ShopUser { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
