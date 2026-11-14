using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Chekni RAW ESC/POS baytlari sifatida termal printerga yuboradi.
/// WPF FlowDocument (grafik) yo'li o'rniga ishlatiladi — "Generic / Text Only" drayveri
/// grafikani qabul qilmaydi va chekni oxirigacha chiqarmaydi/kesmaydi.
/// POS-80 va boshqa standart ESC/POS termal printerlarga mos.
/// </summary>
[SupportedOSPlatform("windows")]
public static class EscPosReceipt
{
    // ── ESC/POS buyruqlari ──────────────────────────────────────────────────
    private static readonly byte[] INIT     = [0x1B, 0x40];       // ESC @  — reset
    private static readonly byte[] BOLD_ON   = [0x1B, 0x45, 0x01];
    private static readonly byte[] BOLD_OFF  = [0x1B, 0x45, 0x00];
    private static readonly byte[] CENTER    = [0x1B, 0x61, 0x01];
    private static readonly byte[] LEFT      = [0x1B, 0x61, 0x00];
    private static readonly byte[] RIGHT     = [0x1B, 0x61, 0x02];
    private static readonly byte[] DOUBLE    = [0x1D, 0x21, 0x11]; // 2x bo'y/en
    private static readonly byte[] NORMAL    = [0x1D, 0x21, 0x00];
    private static readonly byte[] LF        = [0x0A];
    // ESC d 6 — kesishdan oldin 6 qator feed (bosish kallagi/kesgich orasidagi masofa).
    private static readonly byte[] FEED_CUT  = [0x1B, 0x64, 0x06];
    // GS V 1 — qisman kesish (POS-80 da tasdiqlangan; to'liq kesish GS V 0 bu printerda ishlamaydi).
    private static readonly byte[] CUT       = [0x1D, 0x56, 0x01];

    /// <param name="printWidth">Ustun kengligi belgilarda: 42 = 80mm, 32 = 57mm.</param>
    /// <param name="showCodes">false bo'lsa Shtrix/MXIK qatorlari bosilmaydi —
    /// pastdagi "Chop etish" (oddiy nusxa) uchun qisqa ko'rinish.</param>
    public static byte[] BuildReceipt(ReceiptData r, int printWidth = 42, bool showCodes = true)
    {
        var enc = Encoding.GetEncoding(866); // CP866 — kirill/lotin termal printerlar uchun
        using var ms = new MemoryStream();

        void Write(byte[] b) => ms.Write(b);
        void Line(string text = "") { ms.Write(enc.GetBytes(text)); Write(LF); }
        void Divider() => Line(new string('-', printWidth));

        Write(INIT);

        Write(CENTER);
        Write(DOUBLE);
        Write(BOLD_ON);
        Line(string.IsNullOrWhiteSpace(r.StoreName) ? "PosCloud" : r.StoreName);
        Write(NORMAL);
        Write(BOLD_OFF);
        Write(LEFT);
        Divider();

        // Sarlavha: "Label ⟶ Qiymat" ustunli blok (uzun qiymatlar o'raladi).
        void Labeled(string label, string? value)
        {
            foreach (var ln in ReceiptLayout.LabeledRows(label, value, printWidth))
                Line(ln);
        }
        Labeled("Manzil", r.Address);
        Labeled("Yur.Manzil", r.LegalAddress);
        Labeled("STIR/JSHSHR", r.Stir);
        Labeled("Kassa", r.Kassa);
        Labeled("Smena", r.FiscalShiftNumber);
        Labeled("Sana va vaqt", r.Date.ToString("dd-MM-yyyy HH:mm:ss"));
        Labeled("Chek", r.ReceiptNumber);
        Labeled("Sotuvchi", r.Cashier);
        Divider();

        // Tovarlar bo'limi sarlavhasi.
        Write(CENTER);
        Write(BOLD_ON);
        Line(r.IsRefund ? "Qaytarish" : "Savdo");
        Write(BOLD_OFF);
        Write(LEFT);
        Divider();

        foreach (var l in r.Lines)
        {
            // Bosh qator (qalin): nomi + soni x narx = summa; ostida Shtrix va MXIK + QQS.
            // Har tovardan keyin ajratuvchi chiziq.
            Write(BOLD_ON);
            foreach (var ln in ReceiptLayout.ItemHeaderRows(l, printWidth))
                Line(ln);
            Write(BOLD_OFF);
            Line(ReceiptLayout.ItemVatRow(l, printWidth));
            if (ReceiptLayout.ItemDiscountRow(l) is { } disc)
                Line(disc);

            if (showCodes)
                foreach (var ln in ReceiptLayout.ItemDetailRows(l, printWidth))
                    Line(ln);

            Divider();
        }

        foreach (var ln in ReceiptLayout.RedeemRows(r))
            Line(ln);
        Write(BOLD_ON);
        Write(DOUBLE);
        Line($"JAMI: {r.Total:N0} so'm");
        Write(NORMAL);
        Write(BOLD_OFF);
        Line($"QQS jami: {r.VatTotal:N2}");
        Line($"To'lov: {PaymentLabel(r.Payment)}");
        foreach (var ln in ReceiptLayout.PaymentSplitRows(r))
            Line(ln);
        if (r.IsRefund)
        {
            Write(BOLD_ON);
            foreach (var ln in ReceiptLayout.RefundRows(r))
                Line(ln);
            Write(BOLD_OFF);
        }
        else
        {
            Line($"Berilgan: {r.Paid:N0}");
            Line($"Qaytim: {r.Change:N0}");
        }
        Divider();

        // Bonus karta bloki (mijoz biriktirilgan bo'lsa).
        var loyalty = ReceiptLayout.LoyaltyRows(r).ToList();
        if (loyalty.Count > 0)
        {
            foreach (var ln in loyalty)
                Line(ln);
            Divider();
        }

        // Fiskal belgi + QR (agent fiskalizatsiya qilgach to'ladi).
        // Tashqi fiskal qurilma (ФМ/fleshka) QR'ni o'zi chiqarsa, POS QR'ni bosmaydi —
        // bir chekda ikkita QR / double-registratsiya bo'lmasligi uchun.
        if (!r.ExternalFiscalDevice && !string.IsNullOrEmpty(r.QrCodeUrl))
        {
            Write(CENTER);
            Write(BOLD_ON);
            Line("*** FISKAL CHEK ***");
            Write(BOLD_OFF);
            if (!string.IsNullOrEmpty(r.FiscalReceiptNumber))
                Line($"Chek: {r.FiscalReceiptNumber}");
            if (!string.IsNullOrEmpty(r.FiscalSign))
                Line($"FB: {r.FiscalSign}");
            if (!string.IsNullOrEmpty(r.FiscalModuleId))
                Line($"FM: {r.FiscalModuleId}");
            if (!string.IsNullOrEmpty(r.FiscalRegistrationNumber))
                Line($"Zavod raqami: {r.FiscalRegistrationNumber}");
            Write(BOLD_ON);
            Line("Soliq QR kodi");
            Write(BOLD_OFF);
            Write(BuildQrCode(r.QrCodeUrl));
            Write(LF);
            Line("ofd.soliq.uz orqali tekshiring");
            Line("Siz xaridning 1% miqdorida");
            Line("\"Keshbek\" olish huquqiga");
            Line("ega bo'ldingiz");
            Write(LEFT);
            Divider();
        }

        Write(CENTER);
        Line("Xaridingiz uchun rahmat!");
        Write(LEFT);

        Write(FEED_CUT);
        Write(CUT);

        return ms.ToArray();
    }

    /// <summary>
    /// Smena yakuni (Z-hisobot) cheki: to'lov turlari bo'yicha savdo, har biri
    /// soliq QR kodi olingan (fiskal) va olinmagan (fiskalsiz) qismga ajratilgan.
    /// </summary>
    /// <param name="printWidth">Ustun kengligi belgilarda: 42 = 80mm, 32 = 57mm.</param>
    public static byte[] BuildShiftReport(ShiftReportData r, int printWidth = 42)
    {
        var s = r.Shift;
        var enc = Encoding.GetEncoding(866);
        using var ms = new MemoryStream();

        void Write(byte[] b) => ms.Write(b);
        void Line(string text = "") { ms.Write(enc.GetBytes(text)); Write(LF); }
        void Divider() => Line(new string('-', printWidth));

        // "Chapdagi matn ......... o'ngdagi summa" — bir qatorga tekislab.
        void Row(string left, string right)
        {
            var space = printWidth - left.Length - right.Length;
            Line(left + (space > 0 ? new string(' ', space) : " ") + right);
        }

        Write(INIT);

        Write(CENTER);
        Write(DOUBLE);
        Write(BOLD_ON);
        Line(string.IsNullOrWhiteSpace(r.StoreName) ? "PosCloud" : r.StoreName);
        Write(NORMAL);
        Line("SMENA YAKUNI (Z-HISOBOT)");
        Write(BOLD_OFF);
        Write(LEFT);
        Divider();

        void Labeled(string label, string? value)
        {
            foreach (var ln in ReceiptLayout.LabeledRows(label, value, printWidth))
                Line(ln);
        }
        Labeled("Manzil", r.Address);
        Labeled("STIR/JSHSHR", r.Stir);
        Labeled("Kassa", r.Kassa);
        Labeled("Kassir", string.IsNullOrWhiteSpace(s.CashierName) ? r.Cashier : s.CashierName);
        Labeled("Ochildi", s.OpenedAt.ToLocalTime().ToString("dd-MM-yyyy HH:mm"));
        Labeled("Yopildi", (s.ClosedAt ?? DateTime.UtcNow).ToLocalTime().ToString("dd-MM-yyyy HH:mm"));
        Divider();

        // To'lov turlari — har biri QR kodli / QR kodsiz qismga ajratilgan.
        Write(CENTER);
        Write(BOLD_ON);
        Line("TO'LOV TURLARI");
        Write(BOLD_OFF);
        Write(LEFT);
        Divider();

        if (s.Payments.Count == 0)
        {
            Line("Bu smenada savdo bo'lmadi.");
            Divider();
        }

        foreach (var p in s.Payments)
        {
            Write(BOLD_ON);
            Row($"{p.MethodLabel} ({p.SaleCount} ta)", $"{p.SaleAmount:N0}");
            Write(BOLD_OFF);
            Row($"  QR kodli ({p.FiscalCount} ta)", $"{p.FiscalAmount:N0}");
            Row($"  QR kodsiz ({p.NonFiscalCount} ta)", $"{p.NonFiscalAmount:N0}");
            if (p.RefundCount > 0)
            {
                Row($"  Qaytarish ({p.RefundCount} ta)", $"-{p.RefundAmount:N0}");
                Row("  Sof (netto)", $"{p.NetAmount:N0}");
            }
            Divider();
        }

        // Fiskal yakun
        Write(BOLD_ON);
        Line("FISKAL HOLAT");
        Write(BOLD_OFF);
        Row($"  QR kodli ({s.FiscalSaleCount} ta)", $"{s.FiscalSalesTotal:N0}");
        Row($"  QR kodsiz ({s.NonFiscalSaleCount} ta)", $"{s.NonFiscalSalesTotal:N0}");
        Divider();

        // Umumiy yakun
        Write(BOLD_ON);
        Row($"SAVDO ({s.SaleCount} ta)", $"{s.TotalSales:N0}");
        Write(BOLD_OFF);
        if (s.RefundCount > 0)
            Row($"QAYTARISH ({s.RefundCount} ta)", $"-{s.TotalRefunds:N0}");
        Write(BOLD_ON);
        Write(DOUBLE);
        Line($"JAMI: {s.TotalSales - s.TotalRefunds:N0}");
        Write(NORMAL);
        Write(BOLD_OFF);
        if (s.VatAmount > 0)
            Row("QQS jami", $"{s.VatAmount:N0}");
        Divider();

        // Kassa (naqd) holati
        Write(BOLD_ON);
        Line("KASSA (NAQD)");
        Write(BOLD_OFF);
        Row("  Boshlang'ich", $"{s.OpeningCash:N0}");
        Row("  Kutilgan", $"{s.ExpectedCash:N0}");
        Row("  Sanab olingan", $"{s.CountedCash ?? 0:N0}");
        Row("  Farq", (s.CashDifference >= 0 ? "+" : "") + $"{s.CashDifference:N0}");
        Divider();

        Line();
        Line("Kassir imzosi: ______________");
        Line();

        Write(FEED_CUT);
        Write(CUT);

        return ms.ToArray();
    }

    // ── QR kod (ESC/POS GS ( k — 2D barcode) ────────────────────────────────
    private static readonly byte[] QR_MODEL2 = [0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00];
    // Modul o'lchami 7 nuqta (203 dpi da ~0.875 mm): ofd.soliq.uz havolasi (QR v6, 41 modul)
    // ~36 mm bo'ladi — VMQ-943 talabi kamida 30×30 mm. 5 nuqtada ~26 mm edi (talabdan kichik).
    private static readonly byte[] QR_SIZE7  = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x07];
    private static readonly byte[] QR_ERR_M  = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31];
    private static readonly byte[] QR_PRINT  = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30];

    /// <summary>URL ni QR kodga aylantirib, markazlangan ESC/POS baytlar qaytaradi.</summary>
    private static byte[] BuildQrCode(string url)
    {
        var urlBytes = Encoding.ASCII.GetBytes(url);
        var store = urlBytes.Length + 3;
        var pL = (byte)(store % 256);
        var pH = (byte)(store / 256);

        using var ms = new MemoryStream();
        ms.Write(CENTER);
        ms.Write(QR_MODEL2);
        ms.Write(QR_SIZE7);
        ms.Write(QR_ERR_M);
        // Store data: GS ( k pL pH 31 50 30 [url]
        ms.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30]);
        ms.Write(urlBytes);
        ms.Write(QR_PRINT);
        return ms.ToArray();
    }

    private static string PaymentLabel(string p) => p switch
    {
        "Card" => "Karta",
        "Transfer" => "O'tkazma",
        "Mixed" => "Aralash",
        "Debt" => "Nasiya",
        _ => "Naqd"
    };

    // ── Windows raw printer API (winspool.drv) ──────────────────────────────

    /// <summary>RAW baytlarni nomlangan Windows printeriga yuboradi (drayverni chetlab o'tadi).</summary>
    public static void SendRaw(string printerName, byte[] data, string jobName)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Printer ochilmadi: \"{printerName}\" (Win32={Marshal.GetLastWin32Error()}). " +
                "Printer \"Devices & Printers\" da o'rnatilganligini tekshiring.");

        try
        {
            var di = new DocInfo { pDocName = jobName, pDataType = "RAW" };
            if (StartDocPrinter(hPrinter, 1, di) == 0)
                throw new InvalidOperationException($"StartDocPrinter xato (Win32={Marshal.GetLastWin32Error()})");
            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new InvalidOperationException($"StartPagePrinter xato (Win32={Marshal.GetLastWin32Error()})");
                try
                {
                    var ptr = Marshal.AllocHGlobal(data.Length);
                    try
                    {
                        Marshal.Copy(data, 0, ptr, data.Length);
                        if (!WritePrinter(hPrinter, ptr, data.Length, out _))
                            throw new InvalidOperationException($"WritePrinter xato (Win32={Marshal.GetLastWin32Error()})");
                    }
                    finally { Marshal.FreeHGlobal(ptr); }
                }
                finally { EndPagePrinter(hPrinter); }
            }
            finally { EndDocPrinter(hPrinter); }
        }
        finally { ClosePrinter(hPrinter); }
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int StartDocPrinter(IntPtr hPrinter, int level, [In] DocInfo di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class DocInfo
    {
        [MarshalAs(UnmanagedType.LPTStr)] public string pDocName = "";
        [MarshalAs(UnmanagedType.LPTStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPTStr)] public string pDataType = "RAW";
    }
}
