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
        Add($"Chek: {r.ReceiptNumber}", align: TextAlignment.Center);
        Add(r.Date.ToString("yyyy-MM-dd HH:mm"), align: TextAlignment.Center);
        Add($"Sotuvchi: {r.Cashier}");
        Add(sep);

        const int width = 32; // sep uzunligi — monospace shriftda ustunlar tekis bo'lishi uchun
        foreach (var line in r.Lines)
        {
            // Termal chek bilan bir xil: nom (chap) ... soni x narx = summa (o'ng), bitta qatorda.
            var right = $"{line.Quantity:0.###} x {line.Price:N0} = {line.LineTotal:N0}";
            var nameMax = Math.Max(1, width - right.Length - 1);
            var name = line.Name.Length > nameMax ? line.Name[..nameMax] : line.Name;
            var space = width - name.Length - right.Length;
            Add(name + (space > 0 ? new string(' ', space) : " ") + right);
        }

        Add(sep);
        Add($"JAMI: {r.Total:N0} so'm", bold: true, size: 13);
        Add($"To'lov: {PaymentLabel(r.Payment)}");
        Add($"Berilgan: {r.Paid:N0}");
        Add($"Qaytim: {r.Change:N0}");
        Add(sep);

        // Fiskal belgi + QR (agent fiskalizatsiya qilgach to'ladi).
        if (!string.IsNullOrEmpty(r.QrCodeUrl))
        {
            Add("*** FISKAL CHEK ***", bold: true, align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalReceiptNumber))
                Add($"Fiskal raqam: {r.FiscalReceiptNumber}", align: TextAlignment.Center);
            if (!string.IsNullOrEmpty(r.FiscalSign))
                Add($"FP: {r.FiscalSign}", align: TextAlignment.Center);

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
}
