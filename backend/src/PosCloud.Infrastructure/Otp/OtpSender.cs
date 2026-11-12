using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Infrastructure.Otp;

/// <summary>
/// Kanallar tartibi: Telegram Gateway → Eskiz SMS → (faqat LogCodes=true bo'lsa) log.
/// Telegram'da akkaunti yo'q raqam yoki xatolikda avtomatik SMS'ga o'tadi.
/// </summary>
public class OtpSender : IOtpSender
{
    private readonly OtpSettings _settings;
    private readonly TelegramGatewayClient _telegram;
    private readonly EskizSmsClient _eskiz;
    private readonly ILogger<OtpSender> _logger;

    public OtpSender(IOptions<OtpSettings> settings, TelegramGatewayClient telegram, EskizSmsClient eskiz,
        ILogger<OtpSender> logger)
    {
        _settings = settings.Value;
        _telegram = telegram;
        _eskiz = eskiz;
        _logger = logger;
    }

    public async Task<string> SendAsync(string phone, string code, int ttlSeconds, bool preferSms, CancellationToken ct = default)
    {
        if (!preferSms && _settings.Telegram.IsConfigured)
        {
            try
            {
                if (await _telegram.SendAsync(phone, code, ttlSeconds, ct))
                    return "telegram";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Telegram Gateway orqali kod yuborilmadi, SMS'ga o'tamiz");
            }
        }

        if (_settings.Eskiz.IsConfigured)
        {
            try
            {
                var message = _settings.Eskiz.MessageTemplate.Replace("{code}", code);
                await _eskiz.SendAsync(phone, message, ct);
                return "sms";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Eskiz orqali SMS yuborilmadi");
            }
        }

        if (_settings.LogCodes)
        {
            _logger.LogWarning("[DEV] OTP {Phone}: {Code}", phone, code);
            return "log";
        }

        throw new ValidationException("Kodni yuborib bo'lmadi. Birozdan keyin qayta urinib ko'ring.");
    }
}

/// <summary>https://core.telegram.org/gateway/api — sendVerificationMessage.</summary>
public class TelegramGatewayClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TelegramGatewayClient> _logger;

    public TelegramGatewayClient(HttpClient http, IOptions<OtpSettings> settings, ILogger<TelegramGatewayClient> logger)
    {
        _http = http;
        _logger = logger;
        var s = settings.Value.Telegram;
        _http.BaseAddress = new Uri(s.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <returns>true — Telegram qabul qildi; false — bu raqamga yuborib bo'lmaydi.</returns>
    public async Task<bool> SendAsync(string phone, string code, int ttlSeconds, CancellationToken ct)
    {
        var body = new
        {
            phone_number = phone,
            code,
            ttl = Math.Clamp(ttlSeconds, 30, 3600)
        };

        using var response = await _http.PostAsJsonAsync("sendVerificationMessage", body, ct);
        var result = await response.Content.ReadFromJsonAsync<TelegramResponse>(cancellationToken: ct);

        if (result?.Ok == true) return true;

        _logger.LogInformation("Telegram Gateway rad etdi: {Status} {Error}", (int)response.StatusCode, result?.Error);
        return false;
    }

    private sealed class TelegramResponse
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }
}

/// <summary>https://documenter.getpostman.com/view/663428/RzfmES4z — Eskiz SMS API.</summary>
public class EskizSmsClient
{
    // Token ~30 kun amal qiladi; jarayon bo'yicha bitta nusxa saqlaymiz.
    private static string? _token;
    private static readonly SemaphoreSlim TokenLock = new(1, 1);

    private readonly HttpClient _http;
    private readonly EskizSettings _settings;

    public EskizSmsClient(HttpClient http, IOptions<OtpSettings> settings)
    {
        _http = http;
        _settings = settings.Value.Eskiz;
        _http.BaseAddress = new Uri(_settings.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task SendAsync(string phone, string message, CancellationToken ct)
    {
        var token = await GetTokenAsync(forceRefresh: false, ct);
        using var response = await PostSmsAsync(token, phone, message, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            token = await GetTokenAsync(forceRefresh: true, ct);
            using var retry = await PostSmsAsync(token, phone, message, ct);
            await EnsureOk(retry, ct);
            return;
        }

        await EnsureOk(response, ct);
    }

    private Task<HttpResponseMessage> PostSmsAsync(string token, string phone, string message, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "message/sms/send")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mobile_phone"] = phone.TrimStart('+'),
                ["message"] = message,
                ["from"] = _settings.From
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _http.SendAsync(request, ct);
    }

    private async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh && _token is not null) return _token;

        await TokenLock.WaitAsync(ct);
        try
        {
            if (!forceRefresh && _token is not null) return _token;

            using var response = await _http.PostAsync("auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = _settings.Email,
                ["password"] = _settings.Password
            }), ct);
            await EnsureOk(response, ct);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            _token = doc.RootElement.GetProperty("data").GetProperty("token").GetString()
                     ?? throw new InvalidOperationException("Eskiz token qaytarmadi.");
            return _token;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private static async Task EnsureOk(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Eskiz {(int)response.StatusCode}: {body}");
    }
}
