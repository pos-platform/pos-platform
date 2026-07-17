namespace PosPlatform.Infrastructure.Authentication;

/// <summary>appsettings.json dagi "Jwt" bo'limiga bog'lanadi.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Token amal qilish muddati (daqiqalarda). 24 soat — kassa smenasi to'liq qamrab olinadi.</summary>
    public int ExpiryMinutes { get; set; } = 1440;
}
