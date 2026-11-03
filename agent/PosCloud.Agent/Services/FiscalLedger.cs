using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Services;

/// <summary>
/// Lokal fiskalizatsiya daftari — <b>idempotentlik</b> uchun. Modulda muvaffaqiyatli
/// ro'yxatdan o'tgan har bir <c>SaleId</c> natijasi bilan birga diskka yoziladi.
/// <para>
/// Muammo: agent chekni fiskalizatsiya qilgach natijani backendga yuboradi. Agar shu
/// yuborish yiqilsa (tarmoq/500) yoki backendda saqlanmasa, chek <c>Queued</c> qolib,
/// agent keyingi tsiklda uni <b>qaytadan</b> modulga yuboradi → soliqqa DUBLIKAT
/// haqiqiy chek ketadi (kuzatilgan: bitta chek r=612 va r=613 bilan ikki marta).
/// </para>
/// <para>
/// Yechim: modulga yuborishdan oldin daftar tekshiriladi. Chek allaqachon ro'yxatdan
/// o'tgan bo'lsa — modulga QAYTA yuborilmaydi, saqlangan natija backendga qayta
/// yuboriladi. Shunday qilib backend qachondir qabul qilsa QR paydo bo'ladi, modulda
/// esa hech qachon ikkinchi yozuv paydo bo'lmaydi.
/// </para>
/// Fayl formati — JSON Lines (har satr bitta yozuv), qo'shib borish (append) uchun
/// eng ishonchli: yarim yozilgan satr faqat o'zini buzadi, butun faylni emas.
/// </summary>
public class FiscalLedger
{
    private const string FileName = "fiscal-ledger.jsonl";

    /// <summary>Bundan eski yozuvlar ishga tushishda tozalanadi (fayl cheksiz o'smasin).</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromDays(60);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly ILogger<FiscalLedger> _logger;
    private readonly string _path;
    private readonly ConcurrentDictionary<Guid, FiscalResult> _entries = new();
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public FiscalLedger(IOptions<AgentOptions> options, ILogger<FiscalLedger> logger)
    {
        _logger = logger;

        var dir = string.IsNullOrWhiteSpace(options.Value.LedgerDirectory)
            ? AppContext.BaseDirectory
            : options.Value.LedgerDirectory.Trim();

        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, FileName);

        Load();
    }

    /// <summary>Chek allaqachon modulda ro'yxatdan o'tganmi; o'tgan bo'lsa saqlangan natija.</summary>
    public bool TryGet(Guid saleId, out FiscalResult result) => _entries.TryGetValue(saleId, out result!);

    /// <summary>
    /// Muvaffaqiyatli fiskalizatsiya natijasini daftarga yozadi. Backendga yuborishdan
    /// <b>OLDIN</b> chaqirilishi shart — aks holda yuborish yiqilsa dublikat xavfi qoladi.
    /// </summary>
    public async Task RecordAsync(Guid saleId, FiscalResult result, CancellationToken cancellationToken)
    {
        if (!result.Success)
            return;

        // Xotirada allaqachon bo'lsa qayta yozmaymiz (birinchi natija asosiy hisoblanadi).
        if (!_entries.TryAdd(saleId, result))
            return;

        var entry = new LedgerEntry
        {
            SaleId = saleId,
            RecordedAt = DateTime.UtcNow,
            Result = result
        };

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var line = JsonSerializer.Serialize(entry, _json);
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, cancellationToken);
        }
        catch (Exception ex)
        {
            // Diskka yozib bo'lmasa ham xotiradagi yozuv shu jarayon davomida himoya beradi.
            _logger.LogError(ex, "Fiskal daftarga yozib bo'lmadi: {Path}", _path);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>Diskdagi daftarni xotiraga o'qiydi va eskirgan yozuvlarni tozalaydi.</summary>
    private void Load()
    {
        if (!File.Exists(_path))
        {
            _logger.LogInformation("Fiskal daftar yaratiladi: {Path}", _path);
            return;
        }

        var kept = new List<string>();
        var cutoff = DateTime.UtcNow - Retention;
        var expired = 0;
        var broken = 0;

        foreach (var line in File.ReadLines(_path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            LedgerEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<LedgerEntry>(line, _json);
            }
            catch (JsonException)
            {
                broken++;   // yarim yozilgan satr — tashlab ketamiz
                continue;
            }

            if (entry is null || entry.Result is null) { broken++; continue; }

            if (entry.RecordedAt < cutoff) { expired++; continue; }

            _entries[entry.SaleId] = entry.Result;
            kept.Add(line);
        }

        _logger.LogInformation(
            "Fiskal daftar o'qildi: {Count} ta yozuv (eskirgan: {Expired}, buzuq: {Broken})",
            _entries.Count, expired, broken);

        // Eskirgan/buzuq satrlar bo'lsa faylni siqib qayta yozamiz.
        if (expired > 0 || broken > 0)
        {
            try
            {
                File.WriteAllLines(_path, kept);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fiskal daftarni siqib bo'lmadi (ishlashga xalaqit bermaydi).");
            }
        }
    }

    private sealed class LedgerEntry
    {
        public Guid SaleId { get; set; }
        public DateTime RecordedAt { get; set; }
        public FiscalResult? Result { get; set; }
    }
}
