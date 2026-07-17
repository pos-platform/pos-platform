using PosPlatform.Desktop.Models;

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

    /// <summary>
    /// Tovar bosh qatori: "Nomi (chap)   soni x narx = summa (o'ng)" — bitta qatorda.
    /// Nomi sig'masa o'raladi va summa oxirgi nom qatoriga (sig'sa) yoki alohida
    /// qatorga o'ngga tekislanib tushadi.
    /// </summary>
    public static IEnumerable<string> ItemHeaderRows(ReceiptLine l, int width)
    {
        var amount = $"{l.Quantity:0.###} x {l.Price:N0} = {l.LineTotal:N0}";
        var chunks = WordWrap(l.Name.Trim(), width).ToList();
        if (chunks.Count == 0) chunks.Add("");

        for (var i = 0; i < chunks.Count - 1; i++)
            yield return chunks[i];

        var last = chunks[^1];
        if (last.Length + 1 + amount.Length <= width)
            yield return last + new string(' ', width - last.Length - amount.Length) + amount;
        else
        {
            yield return last;
            yield return amount.PadLeft(width);
        }
    }

    /// <summary>
    /// Tovar blokining kod qatorlari (bosh qatordan keyingi qism):
    ///   Shtrix: 4780047222509
    ///   MXIK: 00403002001000000          QQS 12%: 1 275
    /// Shtrix/MXIK bo'lmasa o'sha qator chiqmaydi.
    /// </summary>
    public static IEnumerable<string> ItemDetailRows(ReceiptLine l, int width)
    {
        if (!string.IsNullOrWhiteSpace(l.Barcode))
            yield return $"Shtrix: {l.Barcode}";

        var mxik = string.IsNullOrWhiteSpace(l.MxikCode) ? "" : $"MXIK: {l.MxikCode}";
        var vat = l.IsVat ? $"QQS 12%: {Math.Round(l.LineTotal * 12m / 112m):N0}" : "";
        foreach (var ln in TwoColumn(mxik, vat, width))
            yield return ln;
    }

    /// <summary>Chap va o'ng ustunni bitta qatorga joylaydi; sig'masa alohida qatorlarga bo'ladi.</summary>
    private static IEnumerable<string> TwoColumn(string left, string right, int width)
    {
        if (left.Length == 0 && right.Length == 0) yield break;
        if (right.Length == 0) { yield return left; yield break; }
        if (left.Length == 0) { yield return right.PadLeft(width); yield break; }

        if (left.Length + 1 + right.Length <= width)
            yield return left + new string(' ', width - left.Length - right.Length) + right;
        else
        {
            yield return left;
            yield return right.PadLeft(width);
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
