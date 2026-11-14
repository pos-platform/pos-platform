using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Infrastructure.Push;

/// <summary>
/// FCM HTTP v1 (https://firebase.google.com/docs/cloud-messaging/send/v1-api).
/// OAuth token Google service account kaliti bilan imzolangan JWT (RS256) orqali olinadi va ~1 soat keshlanadi.
/// Singleton: HttpClient IHttpClientFactory'dan har chaqiruvda olinadi.
/// </summary>
public class FcmPushSender : IPushSender
{
    public const string HttpClientName = "fcm";

    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";
    private const string DefaultTokenUri = "https://oauth2.googleapis.com/token";

    private readonly IHttpClientFactory _httpFactory;
    private readonly FcmSettings _settings;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly Lazy<ServiceAccount> _account;

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTime _accessTokenExpires;

    private readonly ISystemErrorRecorder _errors;

    public FcmPushSender(IHttpClientFactory httpFactory, IOptions<PushSettings> settings, ILogger<FcmPushSender> logger,
        ISystemErrorRecorder errors)
    {
        _httpFactory = httpFactory;
        _settings = settings.Value.Fcm;
        _logger = logger;
        _errors = errors;
        _account = new Lazy<ServiceAccount>(LoadServiceAccount);
    }

    public async Task<PushSendResult> SendAsync(string token, string title, string body,
        IReadOnlyDictionary<string, string> data, CancellationToken ct = default)
    {
        try
        {
            var account = _account.Value;
            var projectId = string.IsNullOrWhiteSpace(_settings.ProjectId) ? account.ProjectId : _settings.ProjectId;

            var payload = new
            {
                message = new
                {
                    token,
                    notification = new { title, body },
                    data,
                    android = new
                    {
                        priority = "high",
                        notification = new { channel_id = _settings.AndroidChannelId, sound = "default" }
                    },
                    apns = new
                    {
                        payload = new { aps = new { sound = "default" } }
                    }
                }
            };

            var url = $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(projectId)}/messages:send";

            using var response = await PostAsync(url, payload, forceTokenRefresh: false, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                using var retry = await PostAsync(url, payload, forceTokenRefresh: true, ct);
                return await ToResult(retry, ct);
            }

            return await ToResult(response, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM push yuborilmadi");
            _errors.Record(SystemErrorEntry.FromException(SystemErrorSource.Background, ex, "FCM push", SystemErrorLevel.Warning));
            return PushSendResult.Failed;
        }
    }

    private async Task<HttpResponseMessage> PostAsync(string url, object payload, bool forceTokenRefresh, CancellationToken ct)
    {
        var accessToken = await GetAccessTokenAsync(forceTokenRefresh, ct);
        var http = _httpFactory.CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await http.SendAsync(request, ct);
    }

    private async Task<PushSendResult> ToResult(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return PushSendResult.Sent;

        var text = await response.Content.ReadAsStringAsync(ct);

        // 404 / UNREGISTERED — ilova o'chirilgan yoki token eskirgan;
        // 400 INVALID_ARGUMENT + "registration token" — token formati noto'g'ri.
        var unregistered = response.StatusCode == HttpStatusCode.NotFound
                           || text.Contains("UNREGISTERED", StringComparison.Ordinal)
                           || (response.StatusCode == HttpStatusCode.BadRequest
                               && text.Contains("registration token", StringComparison.OrdinalIgnoreCase));
        if (unregistered)
        {
            _logger.LogInformation("FCM token yaroqsiz: {Status}", (int)response.StatusCode);
            return PushSendResult.InvalidToken;
        }

        _errors.Record(new SystemErrorEntry
        {
            Source = SystemErrorSource.Background,
            Level = SystemErrorLevel.Warning,
            Message = $"FCM push xatosi: HTTP {(int)response.StatusCode}",
            Details = text.Length > 2000 ? text[..2000] : text,
            ErrorType = "fcm",
            Location = "FcmPushSender"
        });
        _logger.LogWarning("FCM xato: {Status} {Body}", (int)response.StatusCode,
            text.Length > 500 ? text[..500] : text);
        return PushSendResult.Failed;
    }

    private async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh && _accessToken is not null && DateTime.UtcNow < _accessTokenExpires)
            return _accessToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (!forceRefresh && _accessToken is not null && DateTime.UtcNow < _accessTokenExpires)
                return _accessToken;

            var account = _account.Value;
            var assertion = CreateAssertion(account);

            var http = _httpFactory.CreateClient(HttpClientName);
            using var response = await http.PostAsync(account.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            }), ct);

            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Google OAuth {(int)response.StatusCode}: {text}");

            var token = JsonSerializer.Deserialize<OAuthResponse>(text)
                        ?? throw new InvalidOperationException("Google OAuth javobi bo'sh.");
            _accessToken = token.AccessToken ?? throw new InvalidOperationException("Google OAuth access_token qaytarmadi.");
            // Muddati tugashidan 5 daqiqa oldin yangilaymiz.
            _accessTokenExpires = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 300));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>Google OAuth uchun RS256 JWT (https://developers.google.com/identity/protocols/oauth2/service-account#authorizingrequests).</summary>
    private static string CreateAssertion(ServiceAccount account)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = new { alg = "RS256", typ = "JWT" };
        var claims = new
        {
            iss = account.ClientEmail,
            scope = Scope,
            aud = account.TokenUri,
            iat = now,
            exp = now + 3600
        };

        var unsigned = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header)) + "." +
                       Base64Url(JsonSerializer.SerializeToUtf8Bytes(claims));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(account.PrivateKey);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return unsigned + "." + Base64Url(signature);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private ServiceAccount LoadServiceAccount()
    {
        var json = !string.IsNullOrWhiteSpace(_settings.ServiceAccountJson)
            ? _settings.ServiceAccountJson
            : File.ReadAllText(_settings.ServiceAccountPath);

        var file = JsonSerializer.Deserialize<ServiceAccountFile>(json)
                   ?? throw new InvalidOperationException("Push:Fcm service account JSON bo'sh.");
        if (string.IsNullOrWhiteSpace(file.ClientEmail) || string.IsNullOrWhiteSpace(file.PrivateKey))
            throw new InvalidOperationException("Push:Fcm service account'da client_email yoki private_key yo'q.");

        var projectId = string.IsNullOrWhiteSpace(_settings.ProjectId) ? file.ProjectId : _settings.ProjectId;
        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException("Push:Fcm ProjectId topilmadi.");

        return new ServiceAccount(projectId!, file.ClientEmail!, file.PrivateKey!,
            string.IsNullOrWhiteSpace(file.TokenUri) ? DefaultTokenUri : file.TokenUri!);
    }

    private sealed record ServiceAccount(string ProjectId, string ClientEmail, string PrivateKey, string TokenUri);

    private sealed class ServiceAccountFile
    {
        [JsonPropertyName("project_id")] public string? ProjectId { get; set; }
        [JsonPropertyName("client_email")] public string? ClientEmail { get; set; }
        [JsonPropertyName("private_key")] public string? PrivateKey { get; set; }
        [JsonPropertyName("token_uri")] public string? TokenUri { get; set; }
    }

    private sealed class OAuthResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}

/// <summary>FCM sozlanmagan (dev): push yuborilmaydi, faqat logga yoziladi.</summary>
public class LogPushSender : IPushSender
{
    private readonly ILogger<LogPushSender> _logger;

    public LogPushSender(ILogger<LogPushSender> logger)
    {
        _logger = logger;
    }

    public Task<PushSendResult> SendAsync(string token, string title, string body,
        IReadOnlyDictionary<string, string> data, CancellationToken ct = default)
    {
        var shortToken = token.Length > 12 ? token[..12] + "…" : token;
        _logger.LogInformation("[DEV] Push {Token}: {Title} — {Body} {@Data}", shortToken, title, body, data);
        return Task.FromResult(PushSendResult.Sent);
    }
}
