namespace PosPlatform.Desktop.Models;

public class Settings
{
    public string BaseUrl { get; set; } = "https://poscloud.uz";
    public bool PrinterEnabled { get; set; }
    public string? PrinterName { get; set; }
    /// <summary>Kassa stantsiyasi ID (masalan "KASSA-1") — fiskal agent terminal filtri uchun.</summary>
    public string? TerminalId { get; set; }
    /// <summary>Bu kassada fiskal QR alohida qurilma (ФМ/fleshka) tomonidan chiqariladi —
    /// POS o'z QR'ini bosmaydi (ikkita QR / double-registratsiya bo'lmasligi uchun).</summary>
    public bool ExternalFiscalDevice { get; set; }
    public string? ScannerComPort { get; set; }
    public int ScannerBaudRate { get; set; } = 9600;
    /// <summary>Tashkilotning yuridik manzili (Yur.Manzil) — chek sarlavhasida chiqadi.</summary>
    public string? LegalAddress { get; set; }
    /// <summary>Payme shaxsiy o'tkazma havolasi (payme.uz/@username) — O'tkazma to'lovida QR sifatida ko'rsatiladi.</summary>
    public string? PaymeLink { get; set; }
    /// <summary>O'tkazma qabul qilinadigan karta raqami (8600/9860...) — Payme'dan boshqa ilovalar uchun.</summary>
    public string? TransferCard { get; set; }
}
