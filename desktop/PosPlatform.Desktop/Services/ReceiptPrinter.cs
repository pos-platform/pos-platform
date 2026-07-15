using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using PosPlatform.Desktop.Models;

namespace PosPlatform.Desktop.Services;

/// <summary>
/// Chekni Windows printeriga chiqaradi (ESC/POS termal printerlar odatda oddiy
/// Windows printer drayveri sifatida o'rnatiladi). 80mm uchun ~280px kenglik.
/// </summary>
public class ReceiptPrinter
{
    /// <summary>
    /// Chekni termal printerga RAW ESC/POS sifatida yuboradi. FlowDocument (grafik) yo'li
    /// "Generic / Text Only" drayveri bilan chekni oxirigacha chiqarmaydi va kesmaydi,
    /// shuning uchun bu yerda to'g'ridan-to'g'ri ESC/POS baytlari yuboriladi.
    /// </summary>
    public void Print(ReceiptData r, string? printerName)
    {
        var name = ResolvePrinterName(printerName);
        var data = EscPosReceipt.BuildReceipt(r);
        EscPosReceipt.SendRaw(name, data, "Chek " + r.ReceiptNumber);
    }

    /// <summary>Tanlangan printer nomini qaytaradi; bo'sh bo'lsa Windows standart printeri.</summary>
    private static string ResolvePrinterName(string? printerName)
    {
        if (!string.IsNullOrWhiteSpace(printerName))
            return printerName.Trim();

        var def = LocalPrintServer.GetDefaultPrintQueue();
        if (def == null || string.IsNullOrWhiteSpace(def.Name))
            throw new InvalidOperationException(
                "Printer tanlanmagan va standart printer topilmadi. Sozlamalarda printerni tanlang.");
        return def.Name;
    }

    public static FlowDocument BuildDocument(ReceiptData r)
    {
        // Ekrandagi ko'rinish uchun shrift kattaroq — 32 belgilik qator qog'ozning to'liq
        // enini egallaydi, o'ngda bo'sh joy qolmaydi. (Termal RAW chekka ta'sir qilmaydi.)
        var doc = new FlowDocument
        {
            PageWidth = 280,
            PagePadding = new Thickness(6),
            ColumnWidth = 280,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 15
        };

        void Add(string text, bool bold = false, double size = 15, TextAlignment align = TextAlignment.Left)
        {
            var p = new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                FontSize = size,
                TextAlignment = align
            };
            if (bold) p.FontWeight = FontWeights.Bold;
            doc.Blocks.Add(p);
        }

        const int width = 32; // monospace ustun kengligi (sep uzunligi)
        const string sep = "--------------------------------";
        Add(string.IsNullOrWhiteSpace(r.StoreName) ? "PosPlatform" : r.StoreName,
            bold: true, size: 19, align: TextAlignment.Center);
        Add(sep);

        // Sarlavha: "Label ⟶ Qiymat" ustunli blok (uzun qiymatlar o'raladi).
        void Labeled(string label, string? value)
        {
            foreach (var ln in ReceiptLayout.LabeledRows(label, value, width))
                Add(ln);
        }
        Labeled("Manzil", r.Address);
        Labeled("Yur.Manzil", r.LegalAddress);
        Labeled("STIR/JSHSHR", r.Stir);
        Labeled("Kassa", r.Kassa);
        Labeled("Smena", r.FiscalShiftNumber);
        Labeled("Sana va vaqt", r.Date.ToString("dd-MM-yyyy HH:mm:ss"));
        Labeled("Chek", r.ReceiptNumber);
        Labeled("Sotuvchi", r.Cashier);
        Add(sep);

        // Tovarlar bo'limi sarlavhasi.
        Add("Savdo", bold: true, align: TextAlignment.Center);
        Add(sep);
        string Row(string left, string right)
        {
            var space = width - left.Length - right.Length;
            return left + (space > 0 ? new string(' ', space) : " ") + right;
        }

        foreach (var line in r.Lines)
        {
            // 1-qator: nom (chap) ... soni x narx = summa (o'ng), bitta qatorda.
            var amount = $"{line.Quantity:0.###} x {line.Price:N0} = {line.LineTotal:N0}";
            var nameMax = Math.Max(1, width - amount.Length - 1);
            var name = line.Name.Length > nameMax ? line.Name[..nameMax] : line.Name;
            Add(Row(name, amount));

            // 2-qator: Shtrix (bo'lsa) + MXIK (har doim — fiskal chek uchun majburiy).
            var codes = new List<string>();
            if (!string.IsNullOrWhiteSpace(line.Barcode)) codes.Add($"Shtrix: {line.Barcode}");
            codes.Add($"MXIK: {(string.IsNullOrWhiteSpace(line.MxikCode) ? "00000000" : line.MxikCode)}");
            Add("  " + string.Join("   ", codes), size: 13);

            if (line.IsVat)
                Add($"  QQS 12%: {Math.Round(line.LineTotal * 12m / 112m):N0}", size: 13);
        }

        Add(sep);
        Add($"JAMI: {r.Total:N0} so'm", bold: true, size: 17);
        if (r.VatTotal > 0)
            Add($"QQS jami (12%): {r.VatTotal:N0}");
        Add($"To'lov: {PaymentLabel(r.Payment)}");
        Add($"Berilgan: {r.Paid:N0}");
        Add($"Qaytim: {r.Change:N0}");
        Add(sep);

        // Fiskal belgi + QR (agent fiskalizatsiya qilgach to'ladi).
        // Tashqi fiskal qurilma (ФМ/fleshka) QR'ni o'zi chiqarsa, POS QR'ni bosmaydi —
        // bir chekda ikkita QR / double-registratsiya bo'lmasligi uchun.
        if (!r.ExternalFiscalDevice && !string.IsNullOrEmpty(r.QrCodeUrl))
        {
            Add("*** FISKAL CHEK ***", bold: true, align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalReceiptNumber))
                Add($"Chek: {r.FiscalReceiptNumber}", align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalSign))
                Add($"FB: {r.FiscalSign}", align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalModuleId))
                Add($"FM: {r.FiscalModuleId}", align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalRegistrationNumber))
                Add($"RS: {r.FiscalRegistrationNumber}", align: TextAlignment.Center);
            Add("Soliq QR kodi", bold: true, align: TextAlignment.Center);

            var qr = QrImage.Generate(r.QrCodeUrl);
            if (qr is not null)
            {
                var img = new System.Windows.Controls.Image
                {
                    Source = qr,
                    Width = 130,
                    Height = 130,
                    Stretch = Stretch.Uniform
                };
                doc.Blocks.Add(new BlockUIContainer(img) { Margin = new Thickness(0, 4, 0, 4) });
            }
            Add("ofd.soliq.uz orqali tekshiring", size: 12, align: TextAlignment.Center);
            Add("Siz xaridning 1% miqdorida \"Keshbek\"", size: 12, align: TextAlignment.Center);
            Add("olish huquqiga ega bo'ldingiz", size: 12, align: TextAlignment.Center);
            Add(sep);
        }
        else if (!r.ExternalFiscalDevice && r.SaleId is not null)
        {
            Add("Fiskalizatsiya kutilmoqda...", size: 12, align: TextAlignment.Center);
            Add(sep);
        }

        Add("Xaridingiz uchun rahmat!", align: TextAlignment.Center);

        return doc;
    }

    private static string PaymentLabel(string p) => p switch
    {
        "Card" => "Karta",
        "Mixed" => "Aralash",
        _ => "Naqd"
    };
}
