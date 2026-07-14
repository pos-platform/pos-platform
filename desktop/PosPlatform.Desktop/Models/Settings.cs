namespace PosPlatform.Desktop.Models;

public class Settings
{
    public string BaseUrl { get; set; } = "https://poscloud.uz";
    public bool PrinterEnabled { get; set; }
    public string? PrinterName { get; set; }
    /// <summary>Kassa stantsiyasi ID (masalan "KASSA-1") — fiskal agent terminal filtri uchun.</summary>
    public string? TerminalId { get; set; }
    public string? ScannerComPort { get; set; }
    public int ScannerBaudRate { get; set; } = 9600;
}
