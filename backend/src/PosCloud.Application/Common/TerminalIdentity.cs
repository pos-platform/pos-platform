namespace PosCloud.Application.Common;

/// <summary>
/// Kassa stantsiyasi ID (masalan "KASSA-1") bilan ishlash uchun umumiy qoidalar.
/// Desktop Sozlamalaridagi qiymat va SuperAdmin foydalanuvchiga yozgan qiymat qo'lda
/// kiritilgani uchun taqqoslash bo'sh joy va katta-kichik harfga sezgir bo'lmasligi kerak.
/// </summary>
public static class TerminalIdentity
{
    /// <summary>Bo'sh/probel qiymatni null ga aylantiradi, aks holda bo'sh joylarni kesadi.</summary>
    public static string? Normalize(string? terminalId) =>
        string.IsNullOrWhiteSpace(terminalId) ? null : terminalId.Trim();

    /// <summary>Ikki kassa ID bir xilmi ("kassa-1" == " KASSA-1 ").</summary>
    public static bool Matches(string? a, string? b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
}
