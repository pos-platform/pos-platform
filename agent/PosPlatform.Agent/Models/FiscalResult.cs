namespace PosPlatform.Agent.Models;

/// <summary>Backendga qaytariladigan fiskalizatsiya natijasi (DTO bilan mos).</summary>
public class FiscalResult
{
    public bool Success { get; set; }
    public string? FiscalSign { get; set; }
    public string? FiscalModuleId { get; set; }
    /// <summary>Kassa apparatining ro'yxatga olish raqami (RS).</summary>
    public string? FiscalRegistrationNumber { get; set; }
    public string? FiscalReceiptNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTime? FiscalizedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static FiscalResult Failure(string message) => new() { Success = false, ErrorMessage = message };
}
