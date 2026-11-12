using System.ComponentModel.DataAnnotations;

namespace PosCloud.Application.Features.ShopAuth.Dtos;

public class SendOtpRequest
{
    /// <summary>Telefon: "+998901234567", "998901234567" yoki "901234567".</summary>
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>true — Telegram o'rniga darhol SMS yuborilsin.</summary>
    public bool PreferSms { get; set; }
}

public class SendOtpResponse
{
    public string Phone { get; set; } = string.Empty;

    /// <summary>telegram | sms | log</summary>
    public string Channel { get; set; } = string.Empty;

    public int CodeLength { get; set; }
    public int ExpiresInSeconds { get; set; }

    /// <summary>Shuncha soniyadan keyin qayta so'rash mumkin.</summary>
    public int ResendAfterSeconds { get; set; }
}

public class VerifyOtpRequest
{
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    [MaxLength(200)]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ShopAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public ShopUserDto User { get; set; } = new();
}

public class ShopUserDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>false — ilova ism so'rash ekranini ko'rsatadi.</summary>
    public bool ProfileCompleted { get; set; }
}
