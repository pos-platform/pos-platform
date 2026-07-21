using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// O'zbekiston OFD (Onlayn Fiskal Qurilma) API orqali haqiqiy fiskalizatsiya.
/// <para>
/// Sozlash: appsettings.json ichida <c>Agent:UseSimulatedFiscal = false</c>,
/// <c>Agent:OfdApiUrl</c> va <c>Agent:OfdApiToken</c> belgilang.
/// </para>
/// Endpointlar Soliq Qo'mitasi hujjatlaridan olingan (CRPTS 2.0).
/// </summary>
public class OfdFiscalDevice : IFiscalDevice
{
    private readonly AgentOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<OfdFiscalDevice> _logger;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public OfdFiscalDevice(IOptions<AgentOptions> options, IHttpClientFactory factory, ILogger<OfdFiscalDevice> logger)
    {
        _options = options.Value;
        _http = factory.CreateClient("ofd");
        _logger = logger;
    }

    public async Task<FiscalResult> FiscalizeAsync(PendingReceipt receipt, CancellationToken cancellationToken)
    {
        try
        {
            var payload = BuildPayload(receipt);
            var json = JsonSerializer.Serialize(payload, _json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.OfdApiToken);

            var response = await _http.PostAsync(
                $"{_options.OfdApiUrl.TrimEnd('/')}/receipts/fiscal",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OFD API xatosi {Status}: {Body}", (int)response.StatusCode, body);
                return FiscalResult.Failure($"OFD API {(int)response.StatusCode}: {body}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var ofdResponse = JsonSerializer.Deserialize<OfdResponse>(responseBody, _json);
            if (ofdResponse is null)
                return FiscalResult.Failure("OFD bo'sh javob qaytardi");

            return new FiscalResult
            {
                Success = true,
                FiscalSign = ofdResponse.FiscalSign,
                FiscalModuleId = ofdResponse.ModuleId ?? _options.FiscalModuleId,
                FiscalRegistrationNumber = string.IsNullOrWhiteSpace(ofdResponse.RegistrationNumber)
                    ? _options.FiscalRegistrationNumber
                    : ofdResponse.RegistrationNumber,
                FiscalReceiptNumber = ofdResponse.FiscalReceiptNumber ?? receipt.ReceiptNumber.Replace("R-", "F-"),
                QrCodeUrl = ofdResponse.QrCodeUrl
                    ?? $"https://ofd.soliq.uz/check?t={_options.FiscalModuleId}&r={ofdResponse.FiscalReceiptNumber}&s={ofdResponse.FiscalSign}",
                FiscalizedAt = ofdResponse.FiscalizedAt ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OFD API chaqiruvida xato");
            return FiscalResult.Failure(ex.Message);
        }
    }

    private object BuildPayload(PendingReceipt receipt) => new
    {
        receiptNumber = receipt.ReceiptNumber,
        receiptType = "SALE",
        paymentType = receipt.PaymentMethod == "Cash" ? "CASH" : "CARD",
        totalAmount = receipt.TotalAmount,
        vatAmount = receipt.VatAmount,
        cashierDevice = _options.FiscalModuleId,
        items = receipt.Items.Select(i => new
        {
            name = i.ProductName,
            mxikCode = i.MxikCode,
            barcode = i.Barcode,
            markingCode = i.MarkingCode,
            unit = i.Unit,
            quantity = i.Quantity,
            price = i.UnitPrice,
            vatPercent = i.VatPercent,
            vatAmount = i.VatAmount,
            total = i.LineTotal
        }).ToArray()
    };

    private sealed class OfdResponse
    {
        public string FiscalSign { get; set; } = string.Empty;
        public string? ModuleId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? FiscalReceiptNumber { get; set; }
        public string? QrCodeUrl { get; set; }
        public DateTime? FiscalizedAt { get; set; }
    }
}
