namespace PosPlatform.Application.Features.Fiscal.Dtos;

/// <summary>
/// Lokal agent fiskalizatsiyadan keyin backendga qaytaradigan natija
/// (FDS / Rahmat MK javobidan to'ldiriladi).
/// </summary>
public class FiscalResultDto
{
    public bool Success { get; set; }

    public string? FiscalSign { get; set; }
    public string? FiscalModuleId { get; set; }
    public string? FiscalReceiptNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTime? FiscalizedAt { get; set; }

    /// <summary>Success=false bo'lsa xatolik matni.</summary>
    public string? ErrorMessage { get; set; }
}
