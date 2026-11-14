using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Xatoliklarni SuperAdmin'ga yuboradi (CONTRACT-10 §10.4, POST /api/client-errors).
/// Har bir hisobot avval lokal SQLite navbatiga (OfflineStore.PendingClientErrors) yoziladi,
/// keyin fonda yuboriladi — oflaynda yoki server xatosida navbatda qoladi va har daqiqada qayta uriniladi.
/// Bir xil xabar 1 daqiqada bir martadan ko'p yuborilmaydi. Reporter hech qachon xato tashlamaydi;
/// token/parol/karta raqamlari matndan olib tashlanadi.
/// </summary>
public static class ErrorReporter
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, DateTime> LastSent = new();
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromMinutes(1);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Shuncha urinishdan keyin hisobot tashlab yuboriladi.</summary>
    private const int MaxAttempts = 50;

    private static OfflineStore? _store;
    private static Timer? _timer;
    private static int _flushing;

    /// <summary>Fon qayta yuborish taymerini ishga tushiradi (App.OnStartup).</summary>
    public static void Start()
    {
        try
        {
            _timer ??= new Timer(_ => _ = FlushAsync(), null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1));
        }
        catch { /* hisobot tizimi ilovani to'xtatmasin */ }
    }

    public static void Report(Exception ex, string level = "Error", string? where = null)
    {
        try
        {
            var msg = (where is null ? "" : where + ": ") + ex.GetType().Name + ": " + ex.Message;
            Report(level, msg, ex.ToString());
        }
        catch { /* hech qachon tashlamaymiz */ }
    }

    /// <param name="level">Warning | Error | Critical.</param>
    public static void Report(string level, string message, string? details = null)
    {
        try
        {
            message = Truncate(Sanitize(message), 1000);
            details = details is null ? null : Truncate(Sanitize(details), 16000);

            var key = level + "|" + message;
            var now = DateTime.UtcNow;
            lock (Gate)
            {
                if (LastSent.TryGetValue(key, out var at) && now - at < DedupeWindow) return;
                LastSent[key] = now;
                if (LastSent.Count > 500)
                    foreach (var k in LastSent.Where(p => now - p.Value >= DedupeWindow).Select(p => p.Key).ToList())
                        LastSent.Remove(k);
            }

            var payload = JsonSerializer.Serialize(new
            {
                source = "Desktop",
                level,
                message,
                details,
                appVersion = SafeVersion(),
                device = SafeDevice()
            }, Json);

            lock (Gate)
            {
                Store()?.EnqueueClientError(payload);
            }
            _ = FlushAsync();
        }
        catch { /* hech qachon tashlamaymiz */ }
    }

    /// <summary>Navbatdagi hisobotlarni yuboradi. Bir vaqtda faqat bitta oqim ishlaydi.</summary>
    public static async Task FlushAsync()
    {
        if (Interlocked.Exchange(ref _flushing, 1) == 1) return;
        try
        {
            List<(string Id, string Payload, int Attempts)> batch;
            lock (Gate)
            {
                batch = Store()?.GetPendingClientErrors() ?? new();
            }

            foreach (var (id, payload, attempts) in batch)
            {
                bool remove, stop = false;
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post,
                        AppState.BaseUrl.TrimEnd('/') + "/api/client-errors")
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };
                    if (!string.IsNullOrEmpty(AppState.Token))
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppState.Token);

                    using var resp = await Http.SendAsync(req).ConfigureAwait(false);
                    var code = (int)resp.StatusCode;
                    // 2xx — yuborildi; 400/404/413/422 — qayta yuborish foyda bermaydi.
                    remove = resp.IsSuccessStatusCode || (code >= 400 && code < 500
                        && resp.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                            or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests));
                    if (!remove) stop = true; // server band/oflayn — keyingi daqiqada
                }
                catch
                {
                    remove = false;
                    stop = true; // tarmoq yo'q
                }

                lock (Gate)
                {
                    var s = Store();
                    if (s is null) return;
                    if (remove || attempts + 1 >= MaxAttempts) s.RemovePendingClientError(id);
                    else s.MarkClientErrorRetry(id);
                }
                if (stop) break;
            }
        }
        catch { /* hech qachon tashlamaymiz */ }
        finally
        {
            Interlocked.Exchange(ref _flushing, 0);
        }
    }

    /// <summary>Reporter o'z ulanishini ishlatadi — UI oqimidagi AppState.Offline bilan to'qnashmasin.</summary>
    private static OfflineStore? Store()
    {
        try { return _store ??= new OfflineStore(); }
        catch { return null; }
    }

    private static readonly Regex[] SecretPatterns =
    [
        new(@"(?i)bearer\s+[A-Za-z0-9\-\._~\+/]+=*", RegexOptions.Compiled),
        new(@"(?i)(""?(?:password|parol|token|accessToken|refreshToken|secret|authorization)""?\s*[:=]\s*)(""[^""]*""|[^\s,;&]+)", RegexOptions.Compiled),
        new(@"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+", RegexOptions.Compiled), // JWT
        new(@"\b\d{4}[ \-]?\d{4}[ \-]?\d{4}[ \-]?\d{4}\b", RegexOptions.Compiled)           // karta raqami
    ];

    private static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var s = text;
        if (!string.IsNullOrEmpty(AppState.Token)) s = s.Replace(AppState.Token, "***");
        s = SecretPatterns[0].Replace(s, "Bearer ***");
        s = SecretPatterns[1].Replace(s, m => m.Groups[1].Value + "***");
        s = SecretPatterns[2].Replace(s, "***");
        s = SecretPatterns[3].Replace(s, "**** **** **** ****");
        return s;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static string? SafeVersion()
    {
        try { return UpdateService.CurrentVersion.ToString(3); } catch { return null; }
    }

    private static string? SafeDevice()
    {
        try
        {
            var terminal = string.IsNullOrWhiteSpace(AppState.TerminalId) ? "" : $" · {AppState.TerminalId}";
            return $"{Environment.MachineName}{terminal} · {Environment.OSVersion.VersionString}";
        }
        catch { return null; }
    }
}
