namespace PosPlatform.Desktop.Models;

public class Settings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public bool PrinterEnabled { get; set; }
    public string? PrinterName { get; set; }
    public string? ScannerComPort { get; set; }
    public int ScannerBaudRate { get; set; } = 9600;
}
