namespace PosPlatform.Desktop.Services;

/// <summary>Ilova bo'ylab umumiy holat (token, foydalanuvchi, backend klient).</summary>
public static class AppState
{
    public static BackendClient Backend { get; set; } = null!;
    public static OfflineStore Offline { get; set; } = new();

    public static string BaseUrl { get; set; } = "https://poscloud.uz";
    public static string Username { get; set; } = "";

    /// <summary>Joriy foydalanuvchining to'liq ism-sharifi (chekda kassir nomi). Bo'sh bo'lsa Username ishlatiladi.</summary>
    public static string CashierName { get; set; } = "";

    /// <summary>Chek sarlavhasida ko'rsatiladigan nom — tashkilot nomi (bo'lmasa filial nomi).</summary>
    public static string StoreName { get; set; } = "";

    /// <summary>Tashkilot STIR (ИНН) — fiskal chek sarlavhasida chiqadi.</summary>
    public static string? OrgStir { get; set; }

    /// <summary>Filial manzili — fiskal chek sarlavhasida chiqadi.</summary>
    public static string? BranchAddress { get; set; }

    /// <summary>Tashkilotning yuridik manzili (Yur.Manzil) — chek sarlavhasida chiqadi.
    /// Sozlamalar oynasidan kiritiladi (%APPDATA% da saqlanadi).</summary>
    public static string? LegalAddress { get; set; }

    /// <summary>O'tkazma qabul qilinadigan karta raqami — O'tkazma to'lovida QR/matn sifatida ko'rsatiladi.</summary>
    public static string? TransferCard { get; set; }
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

    /// <summary>Chekda ko'rsatiladigan kassir nomi (to'liq ism bo'lsa o'sha, aks holda login).</summary>
    public static string CashierDisplay => string.IsNullOrWhiteSpace(CashierName) ? Username : CashierName;

    public static bool IsRestaurant => BusinessType == "Restaurant";

    public static bool IsOnline { get; set; } = true;

    public static bool PrinterEnabled { get; set; }
    public static string? PrinterName { get; set; }

    /// <summary>Kassa stantsiyasi ID (masalan "KASSA-1") — har sotuvda backendga yuboriladi,
    /// fiskal agent shu bo'yicha o'z terminalining cheklarini oladi. Bo'sh bo'lishi mumkin.</summary>
    public static string? TerminalId { get; set; }

    /// <summary>Bu kassada fiskal QR alohida qurilma (ФМ/fleshka) tomonidan chiqariladi.
    /// Yoqilgan bo'lsa POS o'z QR blokini bosmaydi — bir chekda ikkita QR / double-registratsiya
    /// bo'lishining oldini oladi. Butun terminal uchun izchil qo'llaniladi.</summary>
    public static bool ExternalFiscalDevice { get; set; }

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
