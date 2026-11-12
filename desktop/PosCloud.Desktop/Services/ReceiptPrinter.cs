using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

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
    /// <param name="showCodes">false bo'lsa Shtrix/MXIK qatorlari bosilmaydi —
    /// pastdagi "Chop etish" (oddiy nusxa) uchun qisqa ko'rinish.</param>
    public void Print(ReceiptData r, string? printerName, bool showCodes = true)
    {
        var name = ResolvePrinterName(printerName);
        var data = EscPosReceipt.BuildReceipt(r, showCodes: showCodes);
        EscPosReceipt.SendRaw(name, data, "Chek " + r.ReceiptNumber);
    }

    /// <summary>Smena yakuni (Z-hisobot) chekini termal printerga RAW ESC/POS sifatida yuboradi.</summary>
    public void PrintShiftReport(ShiftReportData r, string? printerName)
    {
        var name = ResolvePrinterName(printerName);
        var data = EscPosReceipt.BuildShiftReport(r);
        EscPosReceipt.SendRaw(name, data, "Z-hisobot " + r.Shift.Id);
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

    /// <param name="showFiscalPending">false bo'lsa "Fiskalizatsiya kutilmoqda..." matni
    /// ko'rsatilmaydi (darhol-chop rejimi uchun).</param>
    /// <param name="showCodes">false bo'lsa Shtrix/MXIK qatorlari ko'rsatilmaydi —
    /// pastdagi "Chop etish" (oddiy nusxa) uchun qisqa ko'rinish.</param>
    public static FlowDocument BuildDocument(ReceiptData r, bool showFiscalPending = true, bool showCodes = true)
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
        Add(string.IsNullOrWhiteSpace(r.StoreName) ? "PosCloud" : r.StoreName,
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

        foreach (var line in r.Lines)
        {
            // Bosh qator (qalin): nomi + soni x narx = summa; ostida Shtrix va MXIK + QQS.
            // Tovar qatorlari kichikroq shriftda — nom + summa bitta qatorga sig'adi.
            // Belgi soni shriftga teskari proporsional: N = 32 * 15 / size (sep eniga teng),
            // shunda o'ngga tekislangan qism chek chetiga tekis tushadi.
            foreach (var ln in ReceiptLayout.ItemHeaderRows(line, 37))
                Add(ln, bold: true, size: 13);
            Add(ReceiptLayout.ItemVatRow(line, 40), size: 12);
            if (ReceiptLayout.ItemDiscountRow(line) is { } disc)
                Add(disc, size: 12);

            if (showCodes)
                foreach (var ln in ReceiptLayout.ItemDetailRows(line, 40))
                    Add(ln, size: 12);

            Add(sep);
        }
        foreach (var ln in ReceiptLayout.RedeemRows(r))
            Add(ln);
        Add($"JAMI: {r.Total:N0} so'm", bold: true, size: 17);
        Add($"QQS jami: {r.VatTotal:N2}");
        Add($"To'lov: {PaymentLabel(r.Payment)}");
        foreach (var ln in ReceiptLayout.PaymentSplitRows(r))
            Add(ln);
        Add($"Berilgan: {r.Paid:N0}");
        Add($"Qaytim: {r.Change:N0}");
        Add(sep);

        // Bonus karta bloki (mijoz biriktirilgan bo'lsa).
        var loyalty = ReceiptLayout.LoyaltyRows(r).ToList();
        if (loyalty.Count > 0)
        {
            foreach (var ln in loyalty)
                Add(ln, bold: true);
            Add(sep);
        }

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
                Add($"Zavod raqami: {r.FiscalRegistrationNumber}", align: TextAlignment.Center);
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
        else if (showFiscalPending && !r.ExternalFiscalDevice && r.SaleId is not null)
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
        "Transfer" => "O'tkazma",
        "Mixed" => "Aralash",
        "Debt" => "Nasiya",
        _ => "Naqd"
    };
}
