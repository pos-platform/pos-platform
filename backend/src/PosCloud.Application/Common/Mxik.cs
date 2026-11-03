namespace PosCloud.Application.Common;

/// <summary>
/// MXIK (ИКПУ) — tasnif.soliq.uz klassifikatoridagi mahsulot/xizmat kodi. Fiskal chekda
/// majburiy va aynan 17 ta raqamdan iborat; noto'g'ri kod bilan chek soliqda tekshiruvdan o'tmaydi.
/// </summary>
public static class Mxik
{
    public const int Length = 17;

    /// <summary>Kod aynan 17 ta raqammi.</summary>
    public static bool IsValid(string? code) =>
        code is { Length: Length } && code.All(char.IsAsciiDigit);

    /// <summary>
    /// Kiritilgan kodni tekshirib qaytaradi (bo'sh joylar kesiladi). Noto'g'ri bo'lsa —
    /// foydalanuvchiga tushunarli xabar bilan <see cref="Exceptions.ValidationException"/>.
    /// </summary>
    public static string Require(string? code, string productName)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        if (!IsValid(trimmed))
            throw new Exceptions.ValidationException(
                $"'{productName}': MXIK (ИКПУ) kodi {Length} ta raqamdan iborat bo'lishi shart " +
                $"(tasnif.soliq.uz dan oling). Kiritilgan: '{trimmed}'.");
        return trimmed;
    }
}
