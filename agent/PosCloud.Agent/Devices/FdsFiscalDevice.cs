using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Devices;

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

    /// <summary>Zavod raqami sozlanmagani haqidagi ogohlantirish bir marta yozilsin.</summary>
    private bool _regNumberWarned;

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
    // chek ketsa, ularga qat'iy o'suvchi vaqt beramiz (modul soatidan oldinga o'tmasdan).
    private static readonly SemaphoreSlim _timeGate = new(1, 1);
    private static DateTime _lastReceiptTime = DateTime.MinValue;

    // Modul ichki (ishonchli RTC) soati bilan kompyuter soati orasidagi farq — muvaffaqiyatli
    // chek javobidan (result.DateTime) o'rganiladi. Kompyuter soati orqada bo'lsa bu musbat
    // bo'lib, keyingi cheklar modul soatiga moslanadi va DATETIME_IS_IN_THE_PAST (9030) yo'qoladi.
    private static TimeSpan _moduleOffset = TimeSpan.Zero;

    /// <summary>
    /// OFD ga bir martada yuboriladigan fayl soni. Katta bo'lak (100) drayverda
    /// "request files count N is not equal response files count 0" xatosini beradi —
    /// 10 tadan yuborilganda server har birini Acknowledged qiladi.
    /// </summary>
    private const int SyncBatchSize = 10;

    /// <summary>Bitta sinxronda ko'pi bilan necha bo'lak yuboriladi (10 × 15 = 150 fayl).</summary>
    private const int SyncMaxRounds = 15;

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
            // Qaytarish asl fiskal chekka bog'lanmasa soliqda "yetim" minus chek bo'ladi —
            // bunday chekni modulga umuman yubormaymiz.
            if (receipt.IsRefund && receipt.RefundInfo is null)
                return FiscalResult.Failure(
                    $"Qaytarish {receipt.ReceiptNumber}: asl chekning fiskal rekvizitlari yo'q (RefundInfo)");

            var factoryId = await ResolveFactoryIdAsync(cancellationToken);
            if (string.IsNullOrEmpty(factoryId))
                return FiscalResult.Failure("Fiskal modul topilmadi (FiscalDrive/List bo'sh — modul ulanganmi?)");

            var baseUrl = _options.FiscalDriveServiceUrl.TrimEnd('/');

            // ── 1-bosqich: chekni bazaga yozib TXID olish ──────────────────────
            // Chek body sifatida to'g'ridan-to'g'ri (o'rovsiz), PascalCase maydonlar bilan
            // yuboriladi — Swagger namunasi: { "Time":..., "Type":0, "Items":[...] }.
            var (txid, txidError) = await GetTxIdAsync(baseUrl, factoryId, receipt, cancellationToken);
            if (txid is null)
                return FiscalResult.Failure(txidError!);

            // ── 2-bosqich: TXID bo'yicha modulda ro'yxatga olish ───────────────
            var (registerOk, registerBody) = await RegisterTxidAsync(baseUrl, factoryId, txid.Value, cancellationToken);

            // Modul to'lgan bo'lsa (SEND_ALL_RECEIPTS_FIRST / LOCKED_SYNC_WITH_SERVER) —
            // ro'yxatga olingan cheklarni OFD serverga sinxronlab, bir marta qayta urinamiz.
            if (!registerOk && NeedsSync(registerBody))
            {
                await SyncModuleAsync(baseUrl, factoryId, cancellationToken);

                // Sinxron sekundlar/daqiqalar olishi mumkin — shu vaqtda eski TXID ning vaqti
                // eskirib, modul uni "9030 DATETIME_IS_IN_THE_PAST" bilan rad etadi. Shuning
                // uchun YANGI vaqt bilan yangi TXID olib, o'shani ro'yxatdan o'tkazamiz.
                var (retryTxid, retryError) = await GetTxIdAsync(baseUrl, factoryId, receipt, cancellationToken);
                if (retryTxid is null)
                {
                    _logger.LogWarning("Sinxrondan keyin TXID olinmadi: {Error}", retryError);
                }
                else
                {
                    (registerOk, registerBody) = await RegisterTxidAsync(
                        baseUrl, factoryId, retryTxid.Value, cancellationToken);
                }
            }

            if (!registerOk)
            {
                _logger.LogWarning("FiscalDriveService RegisterTXID muvaffaqiyatsiz: {Body}", registerBody);
                return FiscalResult.Failure($"Fiskal modul (RegisterTXID): {registerBody}");
            }

            var result = JsonSerializer.Deserialize<ReceiptResult>(registerBody, _json);
            if (result is null || string.IsNullOrEmpty(result.FiscalSign))
                return FiscalResult.Failure($"Fiskal modul natija qaytarmadi (FiscalSign yo'q): {registerBody}");

            // Muvaffaqiyatli chekda modul o'zining (ishonchli RTC) vaqtini qaytaradi. Vaqt-gate'ni
            // shu vaqtga moslaymiz — keyingi cheklar modul soatidan doim oldinda bo'lib,
            // kompyuter soati orqada bo'lsa ham DATETIME_IS_IN_THE_PAST (9030) ni oldini oladi.
            await SeedTimeGateFromModuleAsync(result.DateTime, cancellationToken);

            // Zavod (seriya) raqami chekning majburiy rekviziti (VMQ-943, 19-band) va modul
            // ID sidan (20-band) farq qiladi — sozlanmagan bo'lsa FM ID ni o'rniga qo'ymaymiz.
            if (string.IsNullOrWhiteSpace(_options.FiscalRegistrationNumber) && !_regNumberWarned)
            {
                _regNumberWarned = true;
                _logger.LogWarning(
                    "Agent:FiscalRegistrationNumber sozlanmagan — chekda kassa zavod raqami chiqmaydi (VMQ-943 talabi)");
            }

            return new FiscalResult
            {
                Success = true,
                FiscalSign = result.FiscalSign,
                FiscalModuleId = result.TerminalID,
                FiscalRegistrationNumber = string.IsNullOrWhiteSpace(_options.FiscalRegistrationNumber)
                    ? null
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
    /// GetTXID — chekni drayver bazasiga <b>joriy</b> (qat'iy o'suvchi) vaqt bilan yozib TXID oladi.
    /// Har chaqiruvda yangi vaqt olinadi, shuning uchun sinxrondan keyin ham qayta ishlatish xavfsiz.
    /// </summary>
    private async Task<(long? txid, string? error)> GetTxIdAsync(
        string baseUrl, string factoryId, PendingReceipt receipt, CancellationToken cancellationToken)
    {
        var receiptTime = await NextReceiptTimeAsync(cancellationToken);

        var response = await _http.PostAsJsonAsync(
            $"{baseUrl}/FiscalDrive/Receipt/GetTXID/{factoryId}",
            BuildReceipt(receipt, receiptTime), _json, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("FiscalDriveService GetTXID HTTP {Status}: {Body}", (int)response.StatusCode, body);
            return (null, $"Fiskal modul (GetTXID) HTTP {(int)response.StatusCode}: {body}");
        }

        var txid = ParseTxId(body);
        return txid is null
            ? (null, $"Fiskal modul TXID qaytarmadi: {body}")
            : (txid, null);
    }

    /// <summary>RegisterTXID (form-data TXID) — natija (muvaffaqiyat, javob tanasi) qaytaradi.</summary>
    private async Task<(bool ok, string body)> RegisterTxidAsync(
        string baseUrl, string factoryId, long txid, CancellationToken cancellationToken)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("TXID", txid.ToString(CultureInfo.InvariantCulture))
        });
        using var response = await _http.PostAsync(
            $"{baseUrl}/FiscalDrive/Receipt/RegisterTXID/{factoryId}", form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.IsSuccessStatusCode, body);
    }

    /// <summary>Xato modul to'lgani (OFD ga sinxron kerak)ni bildiradimi.</summary>
    private static bool NeedsSync(string body) =>
        body.Contains("SEND_ALL_RECEIPTS_FIRST", StringComparison.OrdinalIgnoreCase)
        || body.Contains("LOCKED_SYNC_WITH_SERVER", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Modulda to'planган cheklarni OFD serverга yuborishни majburlaydi (modul to'lиб qolганда).
    /// Xatolar log'lanади, lekin fiskalizatsiyani to'xtatmайди — keyin qayta urinamiz.
    /// </summary>
    private async Task SyncModuleAsync(string baseUrl, string factoryId, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Fiskal modul to'ldi — cheklarni OFD serverga sinxronlaymiz...");
        await PostSyncAsync($"{baseUrl}/FiscalDrive/State/Sync/{factoryId}", withItemsCount: false, cancellationToken);
        await DrainAsync($"{baseUrl}/DataBase/Files/Sync/FullReceipts/{factoryId}", cancellationToken);
        await DrainAsync($"{baseUrl}/DataBase/Files/Sync/Receipts/{factoryId}", cancellationToken);
        await DrainAsync($"{baseUrl}/DataBase/Files/Sync/ZReports/{factoryId}", cancellationToken);
    }

    /// <summary>
    /// Yig'ilib qolgan fayllarni <b>kichik bo'laklarda</b> OFD serverga uzatadi.
    /// <para>
    /// Bir martada ko'p fayl so'ralsa drayver
    /// <c>"request files count 81 is not equal response files count 0"</c> bilan yiqiladi
    /// (ayniqsa Wi-Fi ustidan) va modul hech qachon bo'shamaydi. 10 tadan yuborilganda esa
    /// server har birini <c>Acknowledged</c> qiladi — shuning uchun bo'lib-bo'lib uzatamiz.
    /// </para>
    /// Bo'sh javob (yuboriladigan fayl qolmadi) yoki xato bo'lganda to'xtaydi.
    /// </summary>
    private async Task DrainAsync(string url, CancellationToken cancellationToken)
    {
        for (var round = 1; round <= SyncMaxRounds; round++)
        {
            var (ok, sent) = await PostSyncBatchAsync(url, cancellationToken);
            if (!ok || sent == 0)
                return;

            _logger.LogInformation(
                "Sinxron {Url}: {Count} ta fayl yuborildi ({Round}-bosqich)", url, sent, round);
        }
    }

    /// <summary>Bitta bo'lakni yuboradi — (muvaffaqiyat, yuborilgan fayl soni).</summary>
    private async Task<(bool ok, int sent)> PostSyncBatchAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ItemsCount", SyncBatchSize.ToString(CultureInfo.InvariantCulture))
            });

            using var response = await _http.PostAsync(url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sinxron {Url} HTTP {Status}: {Body}", url, (int)response.StatusCode, body);
                return (false, 0);
            }

            return (true, CountSyncedItems(body));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sinxron xatosi: {Url}", url);
            return (false, 0);
        }
    }

    /// <summary>Sinxron javobidagi <c>Items</c> massivi uzunligi (yuborilgan fayl soni).</summary>
    private static int CountSyncedItems(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("Items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
                return items.GetArrayLength();
        }
        catch (JsonException)
        {
            // JSON emas (masalan "OK") — sanab bo'lmadi, davom etmaymiz.
        }

        return 0;
    }

    private async Task PostSyncAsync(string url, bool withItemsCount, CancellationToken cancellationToken)
    {
        try
        {
            HttpContent? content = withItemsCount
                ? new FormUrlEncodedContent(new[]
                    { new KeyValuePair<string, string>("ItemsCount", SyncBatchSize.ToString(CultureInfo.InvariantCulture)) })
                : null;
            using var response = await _http.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Sinxron {Url} HTTP {Status}: {Body}", url, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sinxron xatosi: {Url}", url);
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
    private object BuildReceipt(PendingReceipt r, DateTime receiptTime)
    {
        var items = r.Items.Select(i => new
        {
            Name = i.ProductName,
            SPIC = string.IsNullOrWhiteSpace(i.MxikCode) ? null : i.MxikCode,
            Barcode = string.IsNullOrWhiteSpace(i.Barcode) ? null : i.Barcode,
            // Markirovka (Asl Belgisi) kodi — GS1 DataMatrix ning to'liq satri. OFD standarti bo'yicha
            // chek qatorining "Labels" massiviga yoziladi (VM 833-son qarori); shu orqali tovar
            // Asl Belgisi tizimida oborotdan chiqadi. Kodsiz (oddiy) tovarlarda null (yuborilmaydi).
            Labels = string.IsNullOrWhiteSpace(i.MarkingCode) ? null : new[] { i.MarkingCode! },
            // Qadoq kodi (tasnif.soliq.uz) — MXIK bilan juft; bo'sh bo'lsa yuborilmaydi.
            PackageCode = string.IsNullOrWhiteSpace(i.PackageCode) ? null : i.PackageCode,
            CommissionInfo = BuildCommissionInfo(i.CommitentTin),
            Units = UnitCode(i.Unit),
            Price = ToTiyin(i.LineTotal),                                           // qator jami summasi
            Amount = (long)Math.Round(i.Quantity * 1000m, MidpointRounding.AwayFromZero),
            VAT = ToTiyin(i.VatAmount),
            VATPercent = (int)Math.Round(i.VatPercent),
            Discount = 0L,
            Other = 0L
        }).ToArray();

        var totalTiyin = items.Sum(i => i.Price);
        var (cash, card) = SplitPayment(r, totalTiyin);

        return new
        {
            Time = receiptTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Type = 0,       // 0 = standart savdo cheki
            // 0 = Sale (sotuv); qaytarish — sozlamadagi kod (standart 1) + asl chekka havola.
            Operation = r.IsRefund ? _options.FiscalRefundOperation : 0,
            ReceivedCash = cash,
            ReceivedCard = card,
            RefundInfo = r.IsRefund ? BuildRefundInfo(r.RefundInfo!, "yyyy-MM-dd HH:mm:ss") : null,
            Items = items
        };
    }

    /// <summary>
    /// Qabul qilingan summani (tiyinda) naqd va karta qismiga ajratadi — yig'indisi doim
    /// <paramref name="totalTiyin"/> ga teng (modul jami-blokni shu bo'yicha tekshiradi).
    /// <list type="bullet">
    /// <item>Card, Transfer (Payme/Click/bank o'tkazmasi) — butunlay karta;</item>
    /// <item>Mixed — karta qismi <see cref="PendingReceipt.CardAmount"/> dan, qolgani naqd.
    /// Eski backend karta qismini yubormasa (0) — avvalgidek butunlay karta;</item>
    /// <item>Cash — butunlay naqd.</item>
    /// </list>
    /// Nasiya (Debt) bu yerga tushmaydi — backend uni fiskal navbatga qo'ymaydi.
    /// </summary>
    internal static (long cash, long card) SplitPayment(PendingReceipt r, long totalTiyin)
    {
        if (r.PaymentMethod.Equals("Mixed", StringComparison.OrdinalIgnoreCase))
        {
            var card = ToTiyin(r.CardAmount);
            if (card <= 0 || card > totalTiyin) card = totalTiyin;
            return (totalTiyin - card, card);
        }

        var isCard = r.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase)
                  || r.PaymentMethod.Equals("Transfer", StringComparison.OrdinalIgnoreCase);
        return isCard ? (0L, totalTiyin) : (totalTiyin, 0L);
    }

    /// <summary>
    /// OFD CommissionInfo — komissiya tovarining komitenti: 9 xonali STIR → <c>TIN</c>,
    /// 14 xonali JSHSHIR → <c>PINFL</c>. Komitent bo'lmasa null (maydon yuborilmaydi).
    /// </summary>
    internal static object? BuildCommissionInfo(string? commitentTin)
    {
        var tin = commitentTin?.Trim();
        if (string.IsNullOrEmpty(tin)) return null;
        return tin.Length == 14
            ? new { TIN = (string?)null, PINFL = (string?)tin }
            : new { TIN = (string?)tin, PINFL = (string?)null };
    }

    /// <summary>
    /// OFD RefundInfo — qaytarish qaysi fiskal chekka tegishli ekanini ko'rsatadi. Vaqt modul
    /// formatida, lokal vaqtda (backend UTC saqlaydi).
    /// </summary>
    internal static object BuildRefundInfo(RefundInfo info, string timeFormat) => new
    {
        TerminalID = info.TerminalId,
        ReceiptSeq = info.ReceiptSeq,
        DateTime = DateTime.SpecifyKind(info.DateTime, DateTimeKind.Utc).ToLocalTime()
            .ToString(timeFormat, CultureInfo.InvariantCulture),
        FiscalSign = info.FiscalSign
    };

    private bool _kgUnitWarned;

    /// <summary>Backenddagi Unit nomini modulning o'lchov birligi kodiga aylantiradi.</summary>
    private long UnitCode(string unit)
    {
        if (!unit.Equals("Kilogram", StringComparison.OrdinalIgnoreCase))
            return _options.FiscalUnitCodePiece;

        if (_options.FiscalUnitCodeKilogram > 0)
            return _options.FiscalUnitCodeKilogram;

        if (!_kgUnitWarned)
        {
            _kgUnitWarned = true;
            _logger.LogWarning(
                "Agent:FiscalUnitCodeKilogram sozlanmagan — kg tovar dona kodi bilan yuborilmoqda. " +
                "tasnif.soliq.uz dagi kilogramm kodini appsettings.json ga yozing.");
        }
        return _options.FiscalUnitCodePiece;
    }

    /// <summary>
    /// Chek uchun qat'iy o'suvchi vaqt (sekund aniqligida) — modul Receipt.Time > LastOperationTime
    /// bo'lishini talab qiladi. Vaqt <b>modul soatiga</b> (kompyuter soati + o'rganilgan offset)
    /// moslanadi; ketma-ket cheklar kamida 1 soniya oraliq bilan, modul soatidan sezilarli
    /// oldinga o'tmasdan (kelajak vaqtni modul rad etishi mumkin) yuboriladi.
    /// </summary>
    private static async Task<DateTime> NextReceiptTimeAsync(CancellationToken cancellationToken)
    {
        await _timeGate.WaitAsync(cancellationToken);
        try
        {
            var moduleNow = DateTime.Now + _moduleOffset;
            var nowSec = TruncateToSecond(moduleNow);
            var next = nowSec <= _lastReceiptTime ? _lastReceiptTime.AddSeconds(1) : nowSec;

            // Modul soati "next" ga yetmagan bo'lsa (bir sekundda ko'p chek) qisqa kutamiz —
            // modul soatidan oldinga o'tmaslik uchun. Offset kattaligicha kutib qolmaslik uchun
            // kutish 5 soniyadan oshmaydi (offset o'rganilgach kutish deyarli 0 bo'ladi).
            var wait = next - (DateTime.Now + _moduleOffset);
            if (wait > TimeSpan.Zero && wait < TimeSpan.FromSeconds(5))
                await Task.Delay(wait, cancellationToken);

            _lastReceiptTime = next;
            return next;
        }
        finally
        {
            _timeGate.Release();
        }
    }

    /// <summary>
    /// Muvaffaqiyatli chekda modul o'zining (ishonchli RTC) vaqtini qaytaradi. Shu vaqtdan
    /// modul bilan kompyuter soati orasidagi offsetni o'rganamiz va vaqt-gate'ni modul soatiga
    /// moslaymiz — kompyuter soati orqada bo'lsa ham keyingi cheklar 9030 bermaydi.
    /// </summary>
    private static async Task SeedTimeGateFromModuleAsync(string? moduleDateTime, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(moduleDateTime))
            return;

        string[] formats = { "yyyy-MM-dd HH:mm:ss", "yyyyMMddHHmmss" };
        if (!DateTime.TryParseExact(moduleDateTime, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var modTime))
            return;

        modTime = DateTime.SpecifyKind(modTime, DateTimeKind.Local);

        await _timeGate.WaitAsync(cancellationToken);
        try
        {
            var offset = modTime - TruncateToSecond(DateTime.Now);
            // Faqat modul PC dan sezilarli OLDINDA bo'lsa offsetni qo'llaymiz (shovqindan saqlanish
            // uchun ±2s ni e'tiborsiz qoldiramiz). Modul PC dan orqada bo'lsa offset shart emas.
            if (offset > TimeSpan.FromSeconds(2))
                _moduleOffset = offset;
            else if (offset < TimeSpan.FromSeconds(-2))
                _moduleOffset = TimeSpan.Zero;

            if (modTime > _lastReceiptTime)
                _lastReceiptTime = modTime;
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
