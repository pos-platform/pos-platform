using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Infrastructure.Authentication;

/// <summary>
/// Bonus karta QR kodini HMAC-SHA256 bilan imzolaydi. Kalit — <c>Loyalty:QrSigningKey</c>;
/// bo'sh bo'lsa JWT kalitidan qat'iy yorliq bilan HMAC orqali hosil qilinadi (JWT imzosi bilan aralashmaydi).
/// </summary>
public sealed class LoyaltyQrSigner : ILoyaltyQrSigner
{
    private const string Prefix = "PCB1.";
    private const string DerivationLabel = "PosCloud.Loyalty.QrSigningKey.v1";
    private const int MaxCodeLength = 512;

    private readonly byte[] _key;

    public LoyaltyQrSigner(IConfiguration configuration, IOptions<JwtSettings> jwt)
    {
        var configured = configuration["Loyalty:QrSigningKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _key = Encoding.UTF8.GetBytes(configured);
        }
        else
        {
            var jwtKey = jwt.Value.Key;
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("Loyalty:QrSigningKey ham, Jwt:Key ham berilmagan.");
            _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwtKey), Encoding.UTF8.GetBytes(DerivationLabel));
        }
    }

    public string Sign(Guid shopUserId, Guid organizationId, DateTime expiresAtUtc)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new QrPayload
        {
            U = shopUserId,
            O = organizationId,
            E = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds()
        });
        var body = Base64Url(payload);
        var sig = HMACSHA256.HashData(_key, Encoding.ASCII.GetBytes(Prefix + body));
        return Prefix + body + "." + Base64Url(sig);
    }

    public bool TryVerify(string code, out Guid shopUserId, out Guid organizationId, out DateTime expiresAtUtc)
    {
        shopUserId = Guid.Empty;
        organizationId = Guid.Empty;
        expiresAtUtc = default;

        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();
        if (code.Length > MaxCodeLength || !code.StartsWith(Prefix, StringComparison.Ordinal)) return false;

        var rest = code[Prefix.Length..];
        var dot = rest.IndexOf('.');
        if (dot <= 0 || dot == rest.Length - 1 || rest.IndexOf('.', dot + 1) >= 0) return false;

        var body = rest[..dot];
        byte[] givenSig;
        byte[] payloadBytes;
        try
        {
            givenSig = FromBase64Url(rest[(dot + 1)..]);
            payloadBytes = FromBase64Url(body);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = HMACSHA256.HashData(_key, Encoding.ASCII.GetBytes(Prefix + body));
        if (!CryptographicOperations.FixedTimeEquals(expected, givenSig)) return false;

        QrPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<QrPayload>(payloadBytes);
        }
        catch (JsonException)
        {
            return false;
        }
        if (payload is null || payload.U == Guid.Empty || payload.O == Guid.Empty || payload.E <= 0) return false;

        shopUserId = payload.U;
        organizationId = payload.O;
        try
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(payload.E).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        return true;
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        switch (b.Length % 4)
        {
            case 2: b += "=="; break;
            case 3: b += "="; break;
            case 1: throw new FormatException();
        }
        return Convert.FromBase64String(b);
    }

    private sealed class QrPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("u")] public Guid U { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("o")] public Guid O { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("e")] public long E { get; set; }
    }
}
