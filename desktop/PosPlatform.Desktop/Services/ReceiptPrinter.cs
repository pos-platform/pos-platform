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
        var doc = new FlowDocument
        {
            PageWidth = 280,
            PagePadding = new Thickness(6),
            ColumnWidth = 280,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11
        };

        void Add(string text, bool bold = false, double size = 11, TextAlignment align = TextAlignment.Left)
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

        const string sep = "--------------------------------";
        Add(string.IsNullOrWhiteSpace(r.StoreName) ? "PosPlatform" : r.StoreName,
            bold: true, size: 15, align: TextAlignment.Center);
        if (!string.IsNullOrWhiteSpace(r.Stir))
            Add($"STIR: {r.Stir}", align: TextAlignment.Center);
        if (!string.IsNullOrWhiteSpace(r.Address))
            Add(r.Address, align: TextAlignment.Center);
        Add(sep);

        // Kassa raqami · Smena (mavjud bo'lgani ko'rsatiladi).
        var idParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.Kassa)) idParts.Add($"Kassa: {r.Kassa}");
        if (!string.IsNullOrWhiteSpace(r.FiscalShiftNumber)) idParts.Add($"Smena: {r.FiscalShiftNumber}");
        if (idParts.Count > 0) Add(string.Join("   ", idParts));

        Add($"Chek: {r.ReceiptNumber}");
        Add(r.Date.ToString("yyyy-MM-dd HH:mm"));
        Add($"Sotuvchi: {r.Cashier}");
        Add(sep);

        foreach (var line in r.Lines)
        {
            // Mahsulot bloki: nom → Shtrix/MXIK → soni x narx = summa (o'ng) → QQS.
            Add(line.Name);

            var meta = new List<string>();
            if (!string.IsNullOrWhiteSpace(line.Barcode)) meta.Add($"Shtrix: {line.Barcode}");
            if (HasMxik(line.MxikCode)) meta.Add($"MXIK: {line.MxikCode}");
            if (meta.Count > 0) Add("  " + string.Join("   ", meta), size: 10);

            Add($"{line.Quantity:0.###} x {line.Price:N0} = {line.LineTotal:N0}", align: TextAlignment.Right);

            if (line.IsVat)
                Add($"  QQS 12%: {Math.Round(line.LineTotal * 12m / 112m):N0}", size: 10);
        }

        Add(sep);
        Add($"JAMI: {r.Total:N0} so'm", bold: true, size: 13);
        if (r.VatTotal > 0)
            Add($"QQS jami (12%): {r.VatTotal:N0}");
        Add($"To'lov: {PaymentLabel(r.Payment)}");
        Add($"Berilgan: {r.Paid:N0}");
        Add($"Qaytim: {r.Change:N0}");
        Add(sep);

        // Fiskal belgi + QR (agent fiskalizatsiya qilgach to'ladi).
        if (!string.IsNullOrEmpty(r.QrCodeUrl))
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
            Add("ofd.soliq.uz orqali tekshiring", size: 9, align: TextAlignment.Center);
            Add("Siz xaridning 1% miqdorida \"Keshbek\"", size: 9, align: TextAlignment.Center);
            Add("olish huquqiga ega bo'ldingiz", size: 9, align: TextAlignment.Center);
            Add(sep);
        }
        else if (r.SaleId is not null)
        {
            Add("Fiskalizatsiya kutilmoqda...", size: 9, align: TextAlignment.Center);
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

    /// <summary>MXIK kodi mazmunli (bo'sh emas va faqat nol emas)mi.</summary>
    private static bool HasMxik(string? code) =>
        !string.IsNullOrWhiteSpace(code) && code.Trim('0').Length > 0;
}
