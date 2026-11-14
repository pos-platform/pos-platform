using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

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
        var amount = $"{l.Quantity:0.###} {l.UnitLabel} x {l.Price:N0} = {l.LineTotal:N0}";
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
    /// Aralash to'lovda naqd va karta qismlari (VMQ-943, 18-band: "nақд, пластик карта ёки аралаш").
    /// Boshqa to'lov turlarida hech narsa qaytmaydi — turning o'zi yetarli.
    /// </summary>
    /// <summary>Ball bilan to'langan qism — JAMI'dan oldin chiqadi (bo'lmasa hech narsa).</summary>
    public static IEnumerable<string> RedeemRows(ReceiptData r)
    {
        if (r.LoyaltyPointsRedeemed <= 0) yield break;
        yield return $"Ball bilan to'landi: -{r.LoyaltyPointsRedeemed:N0}";
    }

    /// <summary>Bonus karta bloki: mijoz telefoni (yashirilgan) va yozilgan ballar.
    /// Mijoz biriktirilmagan savdoda hech narsa qaytmaydi.</summary>
    public static IEnumerable<string> LoyaltyRows(ReceiptData r)
    {
        if (!string.IsNullOrWhiteSpace(r.CustomerMaskedPhone))
            yield return $"Mijoz: {r.CustomerMaskedPhone}";
        if (r.LoyaltyPointsEarned > 0)
            yield return $"Bonus: +{r.LoyaltyPointsEarned:N0} ball";
    }

    /// <summary>Qaytarish cheki bloki (10.5): ayirilgan keshbek, ushlab qolingan pul, mijozga beriladigan pul.
    /// Oddiy savdo chekida hech narsa qaytmaydi.</summary>
    public static IEnumerable<string> RefundRows(ReceiptData r)
    {
        if (!r.IsRefund) yield break;
        if (r.LoyaltyPointsReversed > 0)
            yield return $"Keshbek ayirildi: -{r.LoyaltyPointsReversed:N0} ball";
        if (r.LoyaltyWithheldAmount > 0)
            yield return $"Keshbek ushlab qolindi: {r.LoyaltyWithheldAmount:N0}";
        yield return $"Mijozga qaytarildi: {(r.RefundCashAmount ?? r.Total):N0} so'm";
        if (!string.IsNullOrWhiteSpace(r.RefundReason))
            yield return $"Sabab: {r.RefundReason!.Trim()}";
    }

    /// <summary>Qator chegirmasi ("Chegirma: −1 200") — chegirma bo'lmasa null.</summary>
    public static string? ItemDiscountRow(ReceiptLine l) =>
        l.Discount > 0 ? $"Chegirma: -{l.Discount:N0}" : null;

    public static IEnumerable<string> PaymentSplitRows(ReceiptData r)
    {
        if (r.Payment != "Mixed") yield break;
        yield return $"  - Naqd: {r.Total - r.CardAmount:N0}";
        yield return $"  - Karta: {r.CardAmount:N0}";
    }

    /// <summary>
    /// Tovar QQS qatori: "QQS 12%   1 928,57" — stavka chapda, summa o'ngda.
    /// QQSsiz tovarda ham "QQS 0%  0" chiqadi (VMQ-943 chek namunasi bo'yicha).
    /// </summary>
    public static string ItemVatRow(ReceiptLine l, int width)
    {
        var left = $"QQS {l.VatPercent:0}%";
        var right = $"{l.VatAmount:N2}";
        var space = width - left.Length - right.Length;
        return left + (space > 0 ? new string(' ', space) : " ") + right;
    }

    /// <summary>
    /// Tovar blokining kod qatorlari (bosh qatordan keyingi qism):
    ///   Shtrix: 4780047222509
    ///   МК: 010893527082308521...
    ///   MXIK: 00403002001000000
    /// Shtrix/MXIK/МК bo'lmasa o'sha qator chiqmaydi.
    /// </summary>
    public static IEnumerable<string> ItemDetailRows(ReceiptLine l, int width)
    {
        if (!string.IsNullOrWhiteSpace(l.Barcode))
            yield return $"Shtrix: {l.Barcode}";

        // Markirovka (Asl Belgisi) kodi — DataMatrix ichida ko'rinmas GS1 ajratkichlari (0x1D/FNC1)
        // bo'ladi; termal printerda axlat chiqmasligi uchun boshqaruv belgilarini olib tashlaymiz.
        if (!string.IsNullOrWhiteSpace(l.MarkingCode))
        {
            var clean = new string(l.MarkingCode.Where(ch => !char.IsControl(ch)).ToArray());
            if (clean.Length > 0)
                yield return $"МК: {clean}";
        }

        if (!string.IsNullOrWhiteSpace(l.MxikCode))
            yield return $"MXIK: {l.MxikCode}";

        if (!string.IsNullOrWhiteSpace(l.CommitentTin))
            yield return $"Komitent STIR: {l.CommitentTin}";
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
