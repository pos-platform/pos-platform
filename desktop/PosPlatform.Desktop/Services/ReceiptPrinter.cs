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
    public void Print(ReceiptData r, string? printerName)
    {
        var doc = BuildDocument(r);

        PrintQueue queue;
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            var server = new LocalPrintServer();
            queue = new PrintQueue(server, printerName);
        }
        else
        {
            queue = LocalPrintServer.GetDefaultPrintQueue();
        }

        var pd = new System.Windows.Controls.PrintDialog { PrintQueue = queue };
        doc.PageWidth = pd.PrintableAreaWidth > 0 ? Math.Min(pd.PrintableAreaWidth, 300) : 280;
        pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Chek " + r.ReceiptNumber);
    }

    private static FlowDocument BuildDocument(ReceiptData r)
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
        Add("PosPlatform", bold: true, size: 15, align: TextAlignment.Center);
        Add($"Chek: {r.ReceiptNumber}", align: TextAlignment.Center);
        Add(r.Date.ToString("yyyy-MM-dd HH:mm"), align: TextAlignment.Center);
        Add($"Kassir: {r.Cashier}");
        Add(sep);

        foreach (var line in r.Lines)
        {
            Add(line.Name);
            Add($"  {line.Quantity:0.###} x {line.Price:N0} = {line.LineTotal:N0}", align: TextAlignment.Right);
        }

        Add(sep);
        Add($"JAMI: {r.Total:N0} so'm", bold: true, size: 13);
        Add($"To'lov: {PaymentLabel(r.Payment)}");
        Add($"Berilgan: {r.Paid:N0}");
        Add($"Qaytim: {r.Change:N0}");
        Add(sep);
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
