namespace PosPlatform.Desktop.Services;

/// <summary>
/// Chek matnini tekislash yordamchilari — ESC/POS (RAW) va ekran ko'rinishi (FlowDocument)
/// bir xil "Label ⟶ Qiymat" ustunli formatni ishlatishi uchun umumiy joyda saqlanadi.
/// </summary>
public static class ReceiptLayout
{
    /// <summary>Sarlavhadagi label ustunining kengligi (belgilarda). Eng uzun label + bo'shliq.</summary>
    public const int LabelWidth = 14;

    /// <summary>
    /// "Label       Qiymat" ko'rinishidagi qator(lar)ni qaytaradi. Qiymat uzun bo'lsa
    /// so'zma-so'z o'raladi va davomi qiymat ustuni ostiga tekislanadi.
    /// value bo'sh bo'lsa hech narsa qaytmaydi.
    /// </summary>
    public static IEnumerable<string> LabeledRows(string label, string? value, int width)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;

        var valueWidth = Math.Max(1, width - LabelWidth);
        var first = true;
        foreach (var chunk in WordWrap(value.Trim(), valueWidth))
        {
            var lead = first ? label.PadRight(LabelWidth) : new string(' ', LabelWidth);
            yield return lead + chunk;
            first = false;
        }
    }

    /// <summary>Matnni berilgan kenglikka so'zma-so'z o'raydi; juda uzun so'zlar kesib bo'linadi.</summary>
    public static IEnumerable<string> WordWrap(string text, int width)
    {
        if (width < 1) width = 1;
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = word;
            // O'z-o'zidan kenglikdan uzun so'z — bo'laklarga bo'lamiz.
            while (w.Length > width)
            {
                if (line.Length > 0) { yield return line; line = ""; }
                yield return w[..width];
                w = w[width..];
            }
            if (line.Length == 0) line = w;
            else if (line.Length + 1 + w.Length <= width) line += " " + w;
            else { yield return line; line = w; }
        }
        if (line.Length > 0) yield return line;
    }
}
