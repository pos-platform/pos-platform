namespace PosPlatform.Desktop.Services;

/// <summary>Ilova bo'ylab umumiy holat (token, foydalanuvchi, backend klient).</summary>
public static class AppState
{
    public static BackendClient Backend { get; set; } = null!;
    public static string BaseUrl { get; set; } = "http://localhost:5000";
    public static string Username { get; set; } = "";
    public static string Role { get; set; } = "";
    public static string Token { get; set; } = "";

    public static bool PrinterEnabled { get; set; }
    public static string? PrinterName { get; set; }

    /// <summary>Joriy sozlamalar (BaseUrl) bilan backend klientni qayta yaratadi.</summary>
    public static void RebuildBackend()
    {
        Backend = new BackendClient(BaseUrl);
        if (!string.IsNullOrEmpty(Token)) Backend.SetToken(Token);
    }
}
