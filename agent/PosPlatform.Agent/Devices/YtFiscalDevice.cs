using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// O'zbekiston fiskal moduli orqali haqiqiy fiskalizatsiya — YT.UZ <c>FiscalDriveAPI</c>
/// lokal drayveri (JSON-RPC 2.0) bilan ishlaydi.
/// <para>
/// Drayver kassa kompyuterida xizmat sifatida ishlaydi va smartcard fiskal modulга
/// murojaat qiladi; chekni imzolatib, soliq qabul qiladigan FiscalSign + QR qaytaradi
/// hamda ma'lumotni OFD (ofd.soliq.uz) serverlariga yuboradi.
/// </para>
/// Sozlash: <c>Agent:FiscalMode = "YtModule"</c>, <c>Agent:FiscalDriveApiUrl = http://127.0.0.1:3448/rpc/api</c>.
/// <para>
/// Pul qiymatlari modulга <b>tiyin</b>da (so'm × 100), miqdor <b>×1000</b> (uch kasr) yuboriladi.
/// </para>
/// </summary>
public class YtFiscalDevice : IFiscalDevice
{
    private readonly AgentOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<YtFiscalDevice> _logger;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public YtFiscalDevice(IOptions<AgentOptions> options, IHttpClientFactory factory, ILogger<YtFiscalDevice> logger)
    {
        _options = options.Value;
        _http = factory.CreateClient("yt-fiscal");
        _logger = logger;
    }

    public async Task<FiscalResult> FiscalizeAsync(PendingReceipt receipt, CancellationToken cancellationToken)
    {
        try
        {
            var request = new JsonRpcRequest
            {
                Method = "Api.SendSaleReceipt",
                Params = new object[] { new { Receipt = BuildReceipt(receipt) } }
            };

            var response = await _http.PostAsJsonAsync(_options.FiscalDriveApiUrl, request, _json, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("FiscalDriveAPI HTTP {Status}: {Body}", (int)response.StatusCode, body);
                return FiscalResult.Failure($"Fiskal modul HTTP {(int)response.StatusCode}: {body}");
            }

            var rpc = JsonSerializer.Deserialize<JsonRpcResponse>(body, _json);
            if (rpc is null)
                return FiscalResult.Failure("Fiskal modul bo'sh javob qaytardi");

            if (rpc.Error is not null)
            {
                var msg = $"{rpc.Error.Message} {rpc.Error.Data}".Trim();
                _logger.LogWarning("FiscalDriveAPI xato {Code}: {Msg}", rpc.Error.Code, msg);
                return FiscalResult.Failure($"Fiskal modul: {msg}");
            }

            if (rpc.Result is null || string.IsNullOrEmpty(rpc.Result.FiscalSign))
                return FiscalResult.Failure("Fiskal modul natija qaytarmadi (FiscalSign yo'q)");

            return new FiscalResult
            {
                Success = true,
                FiscalSign = rpc.Result.FiscalSign,
                FiscalModuleId = rpc.Result.TerminalID,
                FiscalRegistrationNumber = string.IsNullOrWhiteSpace(rpc.Result.RegisterNumber)
                    ? _options.FiscalRegistrationNumber
                    : rpc.Result.RegisterNumber,
                FiscalReceiptNumber = rpc.Result.ReceiptSeq,
                QrCodeUrl = rpc.Result.QRCodeURL,
                FiscalizedAt = ParseDateTime(rpc.Result.DateTime)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FiscalDriveAPI chaqiruvida xato");
            return FiscalResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// PendingReceipt'ni YT modul kutadigan Receipt obyektiga aylantiradi.
    /// <para>
    /// Modul item uchun <c>Price</c> = qatorning <b>umumiy</b> summasini (LineTotal),
    /// <c>Amount</c> = miqdor × 1000 (faqat ma'lumot uchun) kutadi. Pul — <b>tiyin</b>da
    /// (so'm × 100). Qabul qilingan summa (ReceivedCash/Card) item Price'lari yig'indisiga
    /// teng bo'lishi shart (modul jami-blokni shu bo'yicha tekshiradi).
    /// </para>
    /// </summary>
    private static object BuildReceipt(PendingReceipt r)
    {
        // Transfer (Payme/Click/bank o'tkazmasi) va Mixed (aralash) fiskalda karta to'lovi
        // sifatida ro'yxatdan o'tadi — naqd deb yozish kassa naqd qoldig'ini buzadi.
        // Nasiya (Debt) bu yerga tushmaydi: backend uni fiskal navbatga qo'ymaydi.
        var isCard = r.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase)
                  || r.PaymentMethod.Equals("Transfer", StringComparison.OrdinalIgnoreCase)
                  || r.PaymentMethod.Equals("Mixed", StringComparison.OrdinalIgnoreCase);

        var items = r.Items.Select(i => new
        {
            Name = i.ProductName,
            SPIC = string.IsNullOrWhiteSpace(i.MxikCode) ? null : i.MxikCode,
            Barcode = string.IsNullOrWhiteSpace(i.Barcode) ? null : i.Barcode,
            Units = 1,
            Price = ToTiyin(i.LineTotal),                                           // qator jami summasi
            Amount = (long)Math.Round(i.Quantity * 1000m, MidpointRounding.AwayFromZero),
            VAT = ToTiyin(i.VatAmount),
            VATPercent = (int)Math.Round(i.VatPercent),
            Discount = 0L,
            Other = 0L
        }).ToArray();

        // Qabul qilingan summa = item Price'lari yig'indisi (jami-blok bilan aniq mos kelishi uchun).
        var totalTiyin = items.Sum(i => i.Price);

        return new
        {
            // Fiskalizatsiya vaqti = hozir (modul "o'tgan vaqt" chekini rad etadi).
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ReceivedCash = isCard ? 0L : totalTiyin,
            ReceivedCard = isCard ? totalTiyin : 0L,
            Items = items
        };
    }

    /// <summary>So'mni tiyinga aylantiradi (× 100).</summary>
    private static long ToTiyin(decimal sum) => (long)Math.Round(sum * 100m, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Modul qaytaradigan lokal "yyyyMMddHHmmss" vaqtini UTC DateTime'ga aylantiradi
    /// (Postgres 'timestamp with time zone' faqat UTC qabul qiladi).
    /// </summary>
    private static DateTime ParseDateTime(string? dt)
    {
        if (!string.IsNullOrWhiteSpace(dt) &&
            DateTime.TryParseExact(dt, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
        return DateTime.UtcNow;
    }

    // ─── JSON-RPC 2.0 modellari ──────────────────────────────────────────────

    private sealed class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("id")] public int Id { get; set; } = 1;
        [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
        [JsonPropertyName("params")] public object[] Params { get; set; } = Array.Empty<object>();
    }

    private sealed class JsonRpcResponse
    {
        [JsonPropertyName("result")] public ReceiptResult? Result { get; set; }
        [JsonPropertyName("error")] public JsonRpcError? Error { get; set; }
    }

    private sealed class JsonRpcError
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("data")] public string? Data { get; set; }
    }

    private sealed class ReceiptResult
    {
        public string? TerminalID { get; set; }
        /// <summary>Kassa ro'yxatga olish raqami (RS) — modul bersa.</summary>
        public string? RegisterNumber { get; set; }
        public string? ReceiptSeq { get; set; }
        public string? DateTime { get; set; }
        public string? FiscalSign { get; set; }
        public string? QRCodeURL { get; set; }
        public string? QRCodeImage64 { get; set; }
    }
}
