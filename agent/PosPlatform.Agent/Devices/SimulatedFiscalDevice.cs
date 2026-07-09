using Microsoft.Extensions.Options;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// Fiskal qurilmaning soxta (simulyatsiya) implementatsiyasi — haqiqiy soliq
/// tizimiga ulanmaydi, soxta fiskal belgi va QR havola yaratadi. Faqat lokal
/// rivojlantirish/test uchun.
/// </summary>
public class SimulatedFiscalDevice : IFiscalDevice
{
    private readonly AgentOptions _options;

    public SimulatedFiscalDevice(IOptions<AgentOptions> options)
    {
        _options = options.Value;
    }

    public Task<FiscalResult> FiscalizeAsync(PendingReceipt receipt, CancellationToken cancellationToken)
    {
        // Haqiqiy qurilma bilan: SDK/HTTP chaqiruvi, FP_SIGN va QR olinadi.
        var fiscalSign = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var fiscalReceiptNumber = receipt.ReceiptNumber.Replace("R-", "F-");

        var result = new FiscalResult
        {
            Success = true,
            FiscalSign = fiscalSign,
            FiscalModuleId = _options.FiscalModuleId,
            FiscalRegistrationNumber = _options.FiscalRegistrationNumber,
            FiscalReceiptNumber = fiscalReceiptNumber,
            QrCodeUrl = $"https://ofd.soliq.uz/check?t={_options.FiscalModuleId}&r={fiscalReceiptNumber}&s={fiscalSign}",
            FiscalizedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }
}
