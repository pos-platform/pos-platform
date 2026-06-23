namespace PosPlatform.Desktop.Services;

/// <summary>Ilova bo'ylab umumiy holat (token, foydalanuvchi, backend klient).</summary>
public static class AppState
{
    public static BackendClient Backend { get; set; } = null!;
    public static OfflineStore Offline { get; set; } = new();

    public static string BaseUrl { get; set; } = "https://poscloud.uz";
    public static string Username { get; set; } = "";
    public static string Role { get; set; } = "";
    public static string Token { get; set; } = "";
    public static string BusinessType { get; set; } = "Store";

    /// <summary>Joriy kassir obunasi shu sanagacha amal qiladi. null = faollashtirilmagan.</summary>
    public static DateTime? LicensedUntil { get; set; }

    /// <summary>Doimiy (bir martalik to'liq sotib olingan) litsenziya — obuna hech qachon tugamaydi.</summary>
    public static bool IsLifetime { get; set; }

    /// <summary>Obuna muddati tugashiga necha kun qolgani (doimiy bo'lsa yoki muddat yo'q bo'lsa null).</summary>
    public static int? LicenseDaysLeft =>
        IsLifetime || LicensedUntil is null
            ? null
            : (int)(LicensedUntil.Value.Date - DateTime.UtcNow.Date).TotalDays;

    public static bool IsRestaurant => BusinessType == "Restaurant";

    public static bool IsOnline { get; set; } = true;

    public static bool PrinterEnabled { get; set; }
    public static string? PrinterName { get; set; }

    public static BarcodeScanner Scanner { get; } = new();
    public static string? ScannerComPort { get; set; }
    public static int ScannerBaudRate { get; set; } = 9600;

    /// <summary>Joriy sozlamalar (BaseUrl) bilan backend klientni qayta yaratadi.</summary>
    public static void RebuildBackend()
    {
        Backend = new BackendClient(BaseUrl);
        if (!string.IsNullOrEmpty(Token)) Backend.SetToken(Token);
    }

    /// <summary>COM port skanerini to'xtatib, qayta ishga tushiradi.</summary>
    public static void RestartScanner()
    {
        Scanner.Stop();
        if (!string.IsNullOrWhiteSpace(ScannerComPort))
        {
            try { Scanner.Start(ScannerComPort, ScannerBaudRate > 0 ? ScannerBaudRate : 9600); }
            catch { /* port mavjud emas yoki band */ }
        }
    }
}
