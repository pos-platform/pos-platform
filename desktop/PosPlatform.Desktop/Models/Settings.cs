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
    /// <summary>O'tkazma qabul qilinadigan karta raqami (8600/9860...) — O'tkazma to'lovida karta ko'rinishida ko'rsatiladi.</summary>
    public string? TransferCard { get; set; }
    /// <summary>Karta amal qilish muddati (MM/YY) — O'tkazma to'lovida karta rasmida ko'rsatiladi.</summary>
    public string? TransferCardExpiry { get; set; }
    /// <summary>Karta egasining ismi (F.I.O) — O'tkazma to'lovida karta rasmida ko'rsatiladi.</summary>
    public string? TransferCardHolder { get; set; }
    /// <summary>Bank nomi (masalan "O'ZBEKISTON MILLIY BANKI") — karta rasmida ko'rsatiladi.</summary>
    public string? TransferCardBank { get; set; }
    /// <summary>Bank logotipi/qisqa belgisi (masalan "NBU") — karta rasmidagi to'q-sariq belgida.</summary>
    public string? TransferCardBankShort { get; set; }
    /// <summary>Karta turi (masalan "PLATINUM", "GOLD") — karta rasmining yuqori-o'ng burchagida.</summary>
    public string? TransferCardType { get; set; }
}
