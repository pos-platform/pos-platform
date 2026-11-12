namespace PosCloud.Infrastructure.Authentication;

/// <summary>appsettings.json dagi "Jwt" bo'limiga bog'lanadi.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Token amal qilish muddati (daqiqalarda). 24 soat — kassa smenasi to'liq qamrab olinadi.</summary>
    public int ExpiryMinutes { get; set; } = 1440;

    /// <summary>Mobil ilova mijozi tokeni audience'i. Xodim audience'idan farq qilishi SHART.</summary>
    public string ShopAudience { get; set; } = "PosCloudShop";

    /// <summary>Mijoz access tokeni muddati (daqiqa). Qisqa — refresh token bilan yangilanadi.</summary>
    public int ShopExpiryMinutes { get; set; } = 60;

    /// <summary>Mijoz refresh tokeni muddati (kun).</summary>
    public int ShopRefreshDays { get; set; } = 60;
}
