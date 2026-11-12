namespace PosCloud.Infrastructure.Otp;

/// <summary>appsettings.json dagi "Otp" bo'limi. Sirlarni env orqali bering: Otp__Telegram__Token va h.k.</summary>
public class OtpSettings
{
    public const string SectionName = "Otp";

    public TelegramGatewaySettings Telegram { get; set; } = new();
    public EskizSettings Eskiz { get; set; } = new();

    /// <summary>
    /// Faqat lokal ishlab chiqish uchun: hech bir kanal sozlanmagan bo'lsa kod logga yoziladi.
    /// Productionda yoqmang.
    /// </summary>
    public bool LogCodes { get; set; }
}

public class TelegramGatewaySettings
{
    /// <summary>gateway.telegram.org dagi API token. Bo'sh bo'lsa Telegram ishlatilmaydi.</summary>
    public string Token { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://gatewayapi.telegram.org/";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Token) && !Token.Contains("CHANGE_ME");
}

public class EskizSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Jo'natuvchi nomi (nickname). Tasdiqlanmagan akkauntda "4546".</summary>
    public string From { get; set; } = "4546";

    /// <summary>
    /// SMS matni. {code} kod bilan almashtiriladi. Matn Eskiz kabinetida shablon sifatida
    /// tasdiqlangan bo'lishi SHART, aks holda SMS yuborilmaydi.
    /// </summary>
    public string MessageTemplate { get; set; } = "PosCloud Market: kirish kodi {code}. Kodni hech kimga bermang.";

    public string BaseUrl { get; set; } = "https://notify.eskiz.uz/api/";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password)
        && !Password.Contains("CHANGE_ME");
}
