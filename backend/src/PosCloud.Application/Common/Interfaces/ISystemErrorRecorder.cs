using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>Yoziladigan xatolik (SystemError'ga guruhlanadi). Matnlar yozishdan oldin tozalanadi.</summary>
public sealed record SystemErrorEntry
{
    public SystemErrorSource Source { get; init; }
    public SystemErrorLevel Level { get; init; } = SystemErrorLevel.Error;
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }

    /// <summary>Xato turi (masalan, exception klassi) — fingerprint uchun.</summary>
    public string? ErrorType { get; init; }

    /// <summary>Joy (birinchi stack qatori yoki vazifa nomi) — fingerprint uchun. Bo'sh bo'lsa Details'dan olinadi.</summary>
    public string? Location { get; init; }

    public string? Path { get; init; }
    public string? Method { get; init; }
    public int? StatusCode { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? AppVersion { get; init; }
    public string? Device { get; init; }
    public string? RequestId { get; init; }

    /// <summary>Exception'dan yozuv (xabar + stack). location — vazifa nomi va h.k. (ixtiyoriy).</summary>
    public static SystemErrorEntry FromException(SystemErrorSource source, Exception ex, string? location = null,
        SystemErrorLevel level = SystemErrorLevel.Error)
    {
        var root = ex is AggregateException { InnerException: { } inner } ? inner : ex;
        return new SystemErrorEntry
        {
            Source = source,
            Level = level,
            Message = string.IsNullOrWhiteSpace(location) ? root.Message : $"{location}: {root.Message}",
            Details = ex.ToString(),
            ErrorType = root.GetType().FullName
            // Location — stack'dan (recorder hisoblaydi); vazifa nomi xabar boshida.
        };
    }
}

/// <summary>
/// Xatoliklarni SuperAdmin uchun saqlaydi (SystemError). Implementatsiya hech qachon xato otmaydi va asosiy
/// so'rovni buzmaydi — alohida scope/DbContext'da yozadi. Sezgir ma'lumot (token, parol, karta) tozalanadi.
/// </summary>
public interface ISystemErrorRecorder
{
    Task RecordAsync(SystemErrorEntry entry, CancellationToken ct = default);

    /// <summary>Kutmasdan yozish (fon vazifasi ichida) — natija kutilmaydi.</summary>
    void Record(SystemErrorEntry entry);
}

/// <summary>Xatolik matnini tozalash va fingerprint hisoblash (yagona qoidalar).</summary>
public static partial class SystemErrorText
{
    public const int MaxMessage = 1000;
    public const int MaxDetails = 16000;

    [GeneratedRegex(@"(?i)bearer\s+[A-Za-z0-9\-\._~\+/]+=*")]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"(?i)\b(password|parol|pwd|passwd|token|access_token|refresh_token|secret|api[_-]?key|otp|pin|cvv|cvc|authorization)(\s*[""']?\s*[=:]\s*[""']?)[^\s&;,""'}]+")]
    private static partial Regex KeyValueRegex();

    [GeneratedRegex(@"\b(?:\d[ -]?){15,18}\d\b")]
    private static partial Regex CardRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(['""]).*?\1")]
    private static partial Regex QuotedRegex();

    /// <summary>Token, parol, karta raqami va h.k. ni yashiradi, uzunlikni cheklaydi.</summary>
    public static string? Scrub(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var s = BearerRegex().Replace(text, "Bearer ***");
        s = JwtRegex().Replace(s, "***");
        s = KeyValueRegex().Replace(s, m => m.Groups[1].Value + m.Groups[2].Value + "***");
        s = CardRegex().Replace(s, "****");
        return s.Length > max ? s[..max] : s;
    }

    /// <summary>So'rov yo'li — query (token bo'lishi mumkin) olib tashlanadi.</summary>
    public static string? CleanPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        var q = path.IndexOf('?');
        var p = q >= 0 ? path[..q] : path;
        return p.Length > 300 ? p[..300] : p;
    }

    /// <summary>source + tur + xabar shabloni (raqam/GUID/qo'shtirnoq o'rniga belgi) + joy → SHA-256 hex.</summary>
    public static string Fingerprint(SystemErrorSource source, string? errorType, string message, string? location)
    {
        var template = GuidRegex().Replace(message, "{id}");
        template = QuotedRegex().Replace(template, "'…'");
        template = NumberRegex().Replace(template, "#");
        if (template.Length > 300) template = template[..300];
        var raw = $"{source}|{errorType}|{template}|{location}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    /// <summary>Stack trace'dan birinchi loyiha qatori ("at PosCloud...."), fayl/qator raqamisiz.</summary>
    public static string? LocationFromDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return null;
        string? first = null;
        foreach (var raw in details.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("at ", StringComparison.Ordinal)) continue;
            var inIdx = line.IndexOf(" in ", StringComparison.Ordinal);
            if (inIdx > 0) line = line[..inIdx];
            first ??= line;
            if (line.Contains("PosCloud", StringComparison.Ordinal)) return Cut(line);
        }
        return first is null ? null : Cut(first);

        static string Cut(string s) => s.Length > 300 ? s[..300] : s;
    }
}
