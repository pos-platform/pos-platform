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
/// O'zbekiston fiskal moduli (ФМ versiya <b>0400</b>) orqali haqiqiy fiskalizatsiya —
/// YT.UZ <c>FiscalDriveService</c> lokal REST-API drayveri bilan ishlaydi.
/// <para>
/// <c>FiscalDriveService</c> — eski <c>FiscalDriveAPI</c> (JSON-RPC, 3448-port) ning o'rnini
/// bosgan dastur. 0400-appletni faqat shu drayver qo'llab-quvvatlaydi; eski FiscalDriveAPI
/// <c>65525: unsupported applet version</c> xatosini beradi.
/// </para>
/// Fiskalizatsiya ikki bosqichda: <c>GetTXID</c> (chekni bazaga yozib TXID olish) →
/// <c>RegisterTXID</c> (TXID bo'yicha modulda ro'yxatga olib FiscalSign + QR olish).
/// <para>
/// Sozlash: <c>Agent:FiscalMode = "FiscalDriveService"</c>,
/// <c>Agent:FiscalDriveServiceUrl = http://127.0.0.1:3449</c>.
/// Pul qiymatlari <b>tiyin</b>da (so'm × 100), miqdor <b>×1000</b> (uch kasr) yuboriladi.
/// </para>
/// </summary>
public class FdsFiscalDevice : IFiscalDevice
{
    private readonly AgentOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<FdsFiscalDevice> _logger;

    /// <summary>Avtomatik topilgan FactoryID keshi (config'da berilmagan bo'lsa).</summary>
    private string? _factoryId;

    // Server (Go) maydon nomlarini PascalCase kutadi (Receipt, Time, Items ...) —
    // camelCase yuborsak validatsiya "required" xatosini beradi. Javobni o'qishда esa
    // katta-kichik harfga bee'tibor bo'lamiz.
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = null,             // PascalCase chiqishi
        PropertyNameCaseInsensitive = true,      // javobni moslashuvchan o'qish
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Modul har chek vaqti oldingisidan QAT'IY katta bo'lishini talab qiladi (sekund aniqligida):
    // Receipt.Time <= LastOperationTime => "receipt time is in the past". Bir sekundда bir nechta
    // chek ketsa, ularga qat'iy o'suvchi vaqt beramiz (real soatdan oldinga o'tmasdan).
    private static readonly SemaphoreSlim _timeGate = new(1, 1);
    private static DateTime _lastReceiptTime = DateTime.MinValue;

    public FdsFiscalDevice(IOptions<AgentOptions> options, IHttpClientFactory factory, ILogger<FdsFiscalDevice> logger)
    {
        _options = options.Value;
        _http = factory.CreateClient("fds-fiscal");
        _logger = logger;
    }

    public async Task<FiscalResult> FiscalizeAsync(PendingReceipt receipt, CancellationToken cancellationToken)
    {
        try
        {
            var factoryId = await ResolveFactoryIdAsync(cancellationToken);
            if (string.IsNullOrEmpty(factoryId))
                return FiscalResult.Failure("Fiskal modul topilmadi (FiscalDrive/List bo'sh — modul ulanganmi?)");

            var baseUrl = _options.FiscalDriveServiceUrl.TrimEnd('/');

            // ── 1-bosqich: chekni bazaga yozib TXID olish ──────────────────────
            // Chek body sifatida to'g'ridan-to'g'ri (o'rovsiz), PascalCase maydonlar bilan
            // yuboriladi — Swagger namunasi: { "Time":..., "Type":0, "Items":[...] }.
            var receiptTime = await NextReceiptTimeAsync(cancellationToken);
            var getTxResponse = await _http.PostAsJsonAsync(
                $"{baseUrl}/FiscalDrive/Receipt/GetTXID/{factoryId}",
                BuildReceipt(receipt, receiptTime), _json, cancellationToken);
            var getTxBody = await getTxResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!getTxResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("FiscalDriveService GetTXID HTTP {Status}: {Body}", (int)getTxResponse.StatusCode, getTxBody);
                return FiscalResult.Failure($"Fiskal modul (GetTXID) HTTP {(int)getTxResponse.StatusCode}: {getTxBody}");
            }

            var txid = ParseTxId(getTxBody);
            if (txid is null)
                return FiscalResult.Failure($"Fiskal modul TXID qaytarmadi: {getTxBody}");

            // ── 2-bosqich: TXID bo'yicha modulda ro'yxatga olish ───────────────
            // TXID form-data (application/x-www-form-urlencoded) sifatida yuboriladi — JSON emas
            // (Swagger: TXID integer, formData). POS-lock yo'q bo'lsa X-POS-Auth header shart emas.
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("TXID", txid.Value.ToString(CultureInfo.InvariantCulture))
            });
            var registerResponse = await _http.PostAsync(
                $"{baseUrl}/FiscalDrive/Receipt/RegisterTXID/{factoryId}", form, cancellationToken);
            var registerBody = await registerResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!registerResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("FiscalDriveService RegisterTXID HTTP {Status}: {Body}", (int)registerResponse.StatusCode, registerBody);
                return FiscalResult.Failure($"Fiskal modul (RegisterTXID) HTTP {(int)registerResponse.StatusCode}: {registerBody}");
            }

            var result = JsonSerializer.Deserialize<ReceiptResult>(registerBody, _json);
            if (result is null || string.IsNullOrEmpty(result.FiscalSign))
                return FiscalResult.Failure($"Fiskal modul natija qaytarmadi (FiscalSign yo'q): {registerBody}");

            return new FiscalResult
            {
                Success = true,
                FiscalSign = result.FiscalSign,
                FiscalModuleId = result.TerminalID,
                FiscalRegistrationNumber = string.IsNullOrWhiteSpace(_options.FiscalRegistrationNumber)
                    ? result.TerminalID
                    : _options.FiscalRegistrationNumber,
                FiscalReceiptNumber = result.ReceiptSeq?.ToString(CultureInfo.InvariantCulture),
                QrCodeUrl = result.QRCodeURL,
                FiscalizedAt = ParseDateTime(result.DateTime)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FiscalDriveService chaqiruvida xato");
            return FiscalResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// FactoryID ni aniqlaydi: config'da berilgan bo'lsa o'shani, aks holda
    /// <c>/FiscalDrive/List</c> dan ulangan modulni (applet 0400 ustuvor) topib keshlaydi.
    /// </summary>
    private async Task<string?> ResolveFactoryIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.FiscalFactoryId))
            return _options.FiscalFactoryId.Trim();

        if (!string.IsNullOrEmpty(_factoryId))
            return _factoryId;

        var baseUrl = _options.FiscalDriveServiceUrl.TrimEnd('/');

        // FiscalDriveService da BARCHA endpointlar POST (GET bo'lsa 405 Method Not Allowed).
        using var listResponse = await _http.PostAsync(
            $"{baseUrl}/FiscalDrive/List", null, cancellationToken);
        var listBody = await listResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!listResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("FiscalDriveService List HTTP {Status}: {Body}", (int)listResponse.StatusCode, listBody);
            return null;
        }

        var modules = JsonSerializer.Deserialize<List<FiscalModuleInfo>>(listBody, _json);
        var chosen = modules?.FirstOrDefault(m => m.AppletVersion == "0400")
                     ?? modules?.FirstOrDefault();

        _factoryId = chosen?.FactoryID;
        if (!string.IsNullOrEmpty(_factoryId))
            _logger.LogInformation("Fiskal modul topildi: FactoryID={Id}, applet={Applet}",
                _factoryId, chosen?.AppletVersion);

        return _factoryId;
    }

    /// <summary>
    /// PendingReceipt'ni FiscalDriveService kutadigan Receipt obyektiga aylantiradi.
    /// <para>
    /// Item <c>Price</c> = qatorning <b>umumiy</b> summasi (LineTotal), <c>Amount</c> = miqdor × 1000.
    /// Pul — <b>tiyin</b>da. Qabul qilingan summa (ReceivedCash/Card) item Price'lari yig'indisiga
    /// teng (drayver: ReceivedCard + ReceivedCash − Σ(Price − Discount − Other) ≤ 100 so'm).
    /// </para>
    /// </summary>
    private static object BuildReceipt(PendingReceipt r, DateTime receiptTime)
    {
        var isCard = r.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase);

        var items = r.Items.Select(i => new
        {
            Name = i.ProductName,
            SPIC = string.IsNullOrWhiteSpace(i.MxikCode) ? null : i.MxikCode,
            Barcode = string.IsNullOrWhiteSpace(i.Barcode) ? null : i.Barcode,
            Units = 1L,
            Price = ToTiyin(i.LineTotal),                                           // qator jami summasi
            Amount = (long)Math.Round(i.Quantity * 1000m, MidpointRounding.AwayFromZero),
            VAT = ToTiyin(i.VatAmount),
            VATPercent = (int)Math.Round(i.VatPercent),
            Discount = 0L,
            Other = 0L
        }).ToArray();

        var totalTiyin = items.Sum(i => i.Price);

        return new
        {
            Time = receiptTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Type = 0,       // 0 = standart savdo cheki
            Operation = 0,  // 0 = Sale (sotuv)
            ReceivedCash = isCard ? 0L : totalTiyin,
            ReceivedCard = isCard ? totalTiyin : 0L,
            Items = items
        };
    }

    /// <summary>
    /// Chek uchun qat'iy o'suvchi vaqt (sekund aniqligida) — modul Receipt.Time > LastOperationTime
    /// bo'lishini talab qiladi. Ketma-ket cheklar kamida 1 soniya oraliq bilan, real soatdan
    /// oldinga o'tmasdan (kelajak vaqtni modul rad etishi mumkin) yuboriladi.
    /// </summary>
    private static async Task<DateTime> NextReceiptTimeAsync(CancellationToken cancellationToken)
    {
        await _timeGate.WaitAsync(cancellationToken);
        try
        {
            var now = TruncateToSecond(DateTime.Now);
            var next = now <= _lastReceiptTime ? _lastReceiptTime.AddSeconds(1) : now;

            // Real soat "next" ga yetmagan bo'lsa kutamiz — kelajak vaqt yubormaymiz.
            var wait = next - DateTime.Now;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);

            _lastReceiptTime = next;
            return next;
        }
        finally
        {
            _timeGate.Release();
        }
    }

    private static DateTime TruncateToSecond(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);

    /// <summary>So'mni tiyinga aylantiradi (× 100).</summary>
    private static long ToTiyin(decimal sum) => (long)Math.Round(sum * 100m, MidpointRounding.AwayFromZero);

    /// <summary>
    /// GetTXID javobidan TXID ni ajratadi — javob oddiy son (<c>123</c>), matnli son
    /// (<c>"123"</c>) yoki <c>{ "TXID": 123 }</c> ko'rinishida bo'lishi mumkin.
    /// </summary>
    private static long? ParseTxId(string body)
    {
        var trimmed = body.Trim().Trim('"');
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var direct))
            return direct;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Number && doc.RootElement.TryGetInt64(out var num))
                return num;
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("TXID", out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var txid))
                    return txid;
                if (prop.ValueKind == JsonValueKind.String &&
                    long.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var txidStr))
                    return txidStr;
            }
        }
        catch (JsonException)
        {
            // JSON emas — pastda null qaytadi.
        }

        return null;
    }

    /// <summary>
    /// FiscalDriveService qaytaradigan "yyyy-MM-dd HH:mm:ss" (lokal) vaqtini UTC'ga aylantiradi
    /// (Postgres 'timestamp with time zone' faqat UTC qabul qiladi). Eski "yyyyMMddHHmmss"
    /// format ham qo'llab-quvvatlanadi.
    /// </summary>
    private static DateTime ParseDateTime(string? dt)
    {
        string[] formats = { "yyyy-MM-dd HH:mm:ss", "yyyyMMddHHmmss" };
        if (!string.IsNullOrWhiteSpace(dt) &&
            DateTime.TryParseExact(dt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
        return DateTime.UtcNow;
    }

    // ─── REST-API modellari ──────────────────────────────────────────────────

    private sealed class FiscalModuleInfo
    {
        public string? ReaderName { get; set; }
        public string? FactoryID { get; set; }
        public string? AppletVersion { get; set; }
    }

    private sealed class ReceiptResult
    {
        public string? TerminalID { get; set; }
        public long? ReceiptSeq { get; set; }
        public string? DateTime { get; set; }
        public string? FiscalSign { get; set; }
        public string? QRCodeURL { get; set; }
    }
}
