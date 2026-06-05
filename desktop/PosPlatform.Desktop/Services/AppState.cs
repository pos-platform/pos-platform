namespace PosPlatform.Desktop.Services;

/// <summary>Ilova bo'ylab umumiy holat (token, foydalanuvchi, backend klient).</summary>
public static class AppState
{
    public static BackendClient Backend { get; set; } = null!;
    public static OfflineStore Offline { get; set; } = new();

    public static string BaseUrl { get; set; } = "http://localhost:5000";
    public static string Username { get; set; } = "";
    public static string Role { get; set; } = "";
    public static string Token { get; set; } = "";

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
