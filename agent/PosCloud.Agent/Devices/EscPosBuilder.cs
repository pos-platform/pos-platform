using System.Text;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Devices;

/// <summary>
/// ESC/POS bayt oqimini yaratadi. TCP va USB printerlar tomonidan ortashliqda ishlatiladi.
/// </summary>
internal static class EscPosBuilder
{
    private static readonly byte[] ESC_INIT     = [0x1B, 0x40];
    private static readonly byte[] ESC_BOLD_ON  = [0x1B, 0x45, 0x01];
    private static readonly byte[] ESC_BOLD_OFF = [0x1B, 0x45, 0x00];
    private static readonly byte[] ESC_CENTER   = [0x1B, 0x61, 0x01];
    private static readonly byte[] ESC_LEFT     = [0x1B, 0x61, 0x00];
    private static readonly byte[] ESC_DOUBLE   = [0x1D, 0x21, 0x11];
    private static readonly byte[] ESC_NORMAL   = [0x1D, 0x21, 0x00];
    private static readonly byte[] ESC_CUT      = [0x1D, 0x56, 0x41, 0x10];
    private static readonly byte[] LF           = [0x0A];
    private static readonly byte[] LF2          = [0x0A, 0x0A];

    // ESC/POS GS ( k — 2D barcode (QR code) commands
    private static readonly byte[] QR_MODEL2    = [0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00];
    // Modul 7 nuqta (203 dpi da ~0.875 mm) — ofd.soliq.uz havolasi uchun QR ~36 mm.
    // VMQ-943: kamida 30×30 mm. Avvalgi 3 nuqta ~14 mm edi.
    private static readonly byte[] QR_SIZE7     = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x07];
    private static readonly byte[] QR_ERR_M     = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31];
    private static readonly byte[] QR_PRINT     = [0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30];

    /// <param name="printWidth">Ustun kengligi belgilarda: 42 = 80mm, 32 = 57mm.</param>
    public static byte[] Build(PendingReceipt receipt, FiscalResult fiscal, int printWidth = 42)
    {
        var enc = Encoding.GetEncoding(866);
        using var ms = new MemoryStream();

        void Write(byte[] b) => ms.Write(b);
        void WriteLine(string text = "") { ms.Write(enc.GetBytes(text)); Write(LF); }
        void Divider() => WriteLine(new string('-', printWidth));
        string PadRight(string left, string right, int width)
        {
            var space = width - left.Length - right.Length;
            return left + (space > 0 ? new string(' ', space) : " ") + right;
        }

        Write(ESC_INIT);

        // Sarlavha — VMQ-943 chek rekvizitlari: nomi (1), manzil (2), STIR (3), sana (4), raqam (5).
        Write(ESC_CENTER);
        Write(ESC_DOUBLE);
        Write(ESC_BOLD_ON);
        WriteLine(string.IsNullOrWhiteSpace(receipt.OrganizationName) ? "CHEK" : receipt.OrganizationName);
        Write(ESC_NORMAL);
        Write(ESC_BOLD_OFF);
        if (!string.IsNullOrWhiteSpace(receipt.Address))
            WriteLine(receipt.Address);
        Write(ESC_LEFT);
        Divider();

        if (!string.IsNullOrWhiteSpace(receipt.OrganizationStir))
            WriteLine(PadRight("STIR:", receipt.OrganizationStir, printWidth));
        WriteLine(PadRight("Sana va vaqt:", receipt.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"), printWidth));
        WriteLine(PadRight(receipt.IsRefund ? "Qaytarish cheki:" : "Savdo cheki:", $"No {receipt.ReceiptNumber}", printWidth));
        Divider();

        // Tovar bloki (6–15): nomi, miqdor + birlik, summa, QQS stavka/summa, MXIK, shtrix,
        // markirovka, komitent STIR.
        foreach (var item in receipt.Items)
        {
            var unit = item.Unit.Equals("Kilogram", StringComparison.OrdinalIgnoreCase) ? "kg" : "d.";
            var name = item.ProductName.Length > printWidth ? item.ProductName[..printWidth] : item.ProductName;
            Write(ESC_BOLD_ON);
            WriteLine(name);
            Write(ESC_BOLD_OFF);
            WriteLine(PadRight($"  {item.Quantity:0.###} {unit} x {item.UnitPrice:N2}", item.LineTotal.ToString("N2"), printWidth));
            WriteLine(PadRight($"  QQS {item.VatPercent:0}%", item.VatAmount.ToString("N2"), printWidth));
            if (!string.IsNullOrWhiteSpace(item.MxikCode))
                WriteLine($"  MXIK: {item.MxikCode}");
            if (!string.IsNullOrWhiteSpace(item.Barcode))
                WriteLine($"  Shtrix: {item.Barcode}");
            if (!string.IsNullOrWhiteSpace(item.MarkingCode))
            {
                // GS1 DataMatrix ichidagi ko'rinmas ajratkichlar (FNC1/0x1D) printerda axlat chiqarmasin.
                var clean = new string(item.MarkingCode.Where(ch => !char.IsControl(ch)).ToArray());
                if (clean.Length > 0)
                    WriteLine($"  MK: {clean}");
            }
            if (!string.IsNullOrWhiteSpace(item.CommitentTin))
                WriteLine($"  Komitent STIR: {item.CommitentTin}");
        }

        Divider();

        // Yakun (16–18): jami, jami QQS, to'lov shakli (aralashda naqd/karta qismlari).
        Write(ESC_BOLD_ON);
        WriteLine(PadRight("JAMI:", $"{receipt.TotalAmount:N2} so'm", printWidth));
        Write(ESC_BOLD_OFF);
        WriteLine(PadRight("QQS jami:", $"{receipt.VatAmount:N2}", printWidth));
        WriteLine(PadRight("To'lov:", receipt.PaymentMethod switch
        {
            "Card" => "Karta",
            "Transfer" => "O'tkazma",
            "Mixed" => "Aralash",
            "Debt" => "Nasiya",
            _ => "Naqd"
        }, printWidth));
        if (receipt.PaymentMethod == "Mixed")
        {
            WriteLine(PadRight("  - Naqd:", $"{receipt.TotalAmount - receipt.CardAmount:N2}", printWidth));
            WriteLine(PadRight("  - Karta:", $"{receipt.CardAmount:N2}", printWidth));
        }

        Divider();

        Write(ESC_CENTER);
        Write(ESC_BOLD_ON);
        WriteLine("*** FISKAL CHEK ***");
        Write(ESC_BOLD_OFF);
        if (!string.IsNullOrEmpty(fiscal.FiscalReceiptNumber))
            WriteLine($"Chek: {fiscal.FiscalReceiptNumber}");
        if (!string.IsNullOrEmpty(fiscal.FiscalSign))
            WriteLine($"FB: {fiscal.FiscalSign}");
        if (!string.IsNullOrEmpty(fiscal.FiscalModuleId))
            WriteLine($"FM: {fiscal.FiscalModuleId}");
        if (!string.IsNullOrEmpty(fiscal.FiscalRegistrationNumber))
            WriteLine($"Zavod raqami: {fiscal.FiscalRegistrationNumber}");

        if (!string.IsNullOrEmpty(fiscal.QrCodeUrl))
        {
            Write(ESC_BOLD_ON);
            WriteLine("Soliq QR kodi");
            Write(ESC_BOLD_OFF);
            Write(LF);
            Write(BuildQrCode(fiscal.QrCodeUrl));
            Write(LF);
            WriteLine("Siz xaridning 1% miqdorida");
            WriteLine("\"Keshbek\" olish huquqiga");
            WriteLine("ega bo'ldingiz");
        }
        Write(ESC_LEFT);

        Write(LF2);
        Write(ESC_CUT);

        return ms.ToArray();
    }

    /// <param name="printWidth">Ustun kengligi belgilarda: 42 = 80mm, 32 = 57mm.</param>
    public static byte[] BuildShiftReport(ShiftReport report, int printWidth = 42)
    {
        var enc = Encoding.GetEncoding(866);
        using var ms = new MemoryStream();

        void Write(byte[] b) => ms.Write(b);
        void WriteLine(string text = "") { ms.Write(enc.GetBytes(text)); Write(LF); }
        void Divider() => WriteLine(new string('-', printWidth));
        string Row(string left, string right)
        {
            var space = printWidth - left.Length - right.Length;
            return left + (space > 0 ? new string(' ', space) : " ") + right;
        }

        Write(ESC_INIT);

        // Sarlavha
        Write(ESC_CENTER);
        Write(ESC_DOUBLE);
        Write(ESC_BOLD_ON);
        WriteLine(report.IsXReport ? "X-HISOBOT" : "Z-HISOBOT");
        Write(ESC_NORMAL);
        Write(ESC_BOLD_OFF);

        Write(ESC_CENTER);
        WriteLine($"Kassir: {report.CashierName ?? "-"}");
        WriteLine($"Ochildi: {report.OpenedAt.ToLocalTime():dd.MM.yyyy HH:mm}");
        WriteLine($"Yopildi: {report.ClosedAt?.ToLocalTime():dd.MM.yyyy HH:mm}");
        Write(ESC_LEFT);
        Divider();

        // Savdo ko'rsatkichlari
        Write(ESC_BOLD_ON);
        WriteLine(Row("SAVDO:", $"{report.TotalSales:N2}"));
        Write(ESC_BOLD_OFF);
        WriteLine(Row($"  Sotuvlar ({report.SaleCount} ta):", $"{report.TotalSales:N2}"));
        WriteLine(Row($"  Qaytarish ({report.RefundCount} ta):", $"-{report.TotalRefunds:N2}"));
        Divider();

        // To'lov turlari — har biri soliq QR kodi olingan/olinmagan qismga ajratilgan.
        Write(ESC_BOLD_ON);
        WriteLine("TO'LOV TURLARI:");
        Write(ESC_BOLD_OFF);

        if (report.Payments.Count > 0)
        {
            foreach (var p in report.Payments)
            {
                Write(ESC_BOLD_ON);
                WriteLine(Row($"{p.MethodLabel} ({p.SaleCount} ta)", $"{p.SaleAmount:N2}"));
                Write(ESC_BOLD_OFF);
                WriteLine(Row($"  QR kodli ({p.FiscalCount} ta):", $"{p.FiscalAmount:N2}"));
                WriteLine(Row($"  QR kodsiz ({p.NonFiscalCount} ta):", $"{p.NonFiscalAmount:N2}"));
                if (p.RefundCount > 0)
                {
                    WriteLine(Row($"  Qaytarish ({p.RefundCount} ta):", $"-{p.RefundAmount:N2}"));
                    WriteLine(Row("  Sof (netto):", $"{p.NetAmount:N2}"));
                }
            }
            Divider();
            WriteLine(Row($"QR kodli jami ({report.FiscalSaleCount} ta):", $"{report.FiscalSalesTotal:N2}"));
            WriteLine(Row($"QR kodsiz jami ({report.NonFiscalSaleCount} ta):", $"{report.NonFiscalSalesTotal:N2}"));
        }
        else
        {
            // Eski backend (kesim yubormaydi) — netto summalar bilan cheklanamiz.
            WriteLine(Row("  Naqd:", $"{report.CashSalesNet:N2}"));
            WriteLine(Row("  Karta:", $"{report.CardSalesNet:N2}"));
            WriteLine(Row("  O'tkazma:", $"{report.TransferSalesNet:N2}"));
            WriteLine(Row("  Nasiya:", $"{report.DebtSalesNet:N2}"));
        }
        Divider();

        // Kassa holati
        Write(ESC_BOLD_ON);
        WriteLine("KASSA:");
        Write(ESC_BOLD_OFF);
        WriteLine(Row("  Boshlangich:", $"{report.OpeningCash:N2}"));
        WriteLine(Row("  Kutilgan:", $"{report.ExpectedCash:N2}"));
        WriteLine(Row("  Sanab:", $"{report.CountedCash:N2}"));
        var diff = report.CashDifference;
        WriteLine(Row("  Farq:", (diff >= 0 ? "+" : "") + $"{diff:N2}"));
        Divider();

        WriteLine(Row("  QQS jami:", $"{report.VatAmount:N2}"));
        Write(LF2);
        Write(ESC_CUT);

        return ms.ToArray();
    }

    /// <summary>Narx yorlig'i (label) — bitta mahsulot uchun n nusxa.</summary>
    public static byte[] BuildLabels(IEnumerable<LabelItem> items, int printWidth = 42)
    {
        var enc = Encoding.GetEncoding(866);
        using var ms = new MemoryStream();

        void Write(byte[] b) => ms.Write(b);
        void WriteLine(string text = "") { ms.Write(enc.GetBytes(text)); Write(LF); }

        Write(ESC_INIT);

        foreach (var item in items)
        {
            for (int i = 0; i < item.Copies; i++)
            {
                Write(ESC_CENTER);
                // Mahsulot nomi (agar uzun bo'lsa qisqartir)
                var name = item.ProductName.Length > printWidth ? item.ProductName[..printWidth] : item.ProductName;
                Write(ESC_BOLD_ON);
                WriteLine(name);
                Write(ESC_BOLD_OFF);

                // Narx — 2x o'lcham
                Write(ESC_DOUBLE);
                Write(ESC_BOLD_ON);
                WriteLine($"{item.Price:N2} so'm");
                Write(ESC_NORMAL);
                Write(ESC_BOLD_OFF);

                // Shtrix-kod matni
                if (!string.IsNullOrEmpty(item.Barcode))
                {
                    WriteLine(item.Barcode);
                    // CODE128 shtrix-kod
                    Write(BuildBarcode128(item.Barcode));
                }

                Write(LF);
                Write(ESC_CUT);
            }
        }

        return ms.ToArray();
    }

    /// <summary>CODE128 shtrix-kodi (GS k m d1...dk).</summary>
    private static byte[] BuildBarcode128(string data)
    {
        var content = "{B" + data; // Code B subset
        var contentBytes = Encoding.ASCII.GetBytes(content);

        using var ms = new MemoryStream();
        // Set barcode height: GS h n
        ms.Write([0x1D, 0x68, 0x50]);
        // Set barcode width: GS w n
        ms.Write([0x1D, 0x77, 0x02]);
        // Print HRI below: GS H 2
        ms.Write([0x1D, 0x48, 0x02]);
        // GS k m=73 (CODE128) n d1...dk
        ms.Write([0x1D, 0x6B, 0x49, (byte)contentBytes.Length]);
        ms.Write(contentBytes);
        ms.Write(LF);
        return ms.ToArray();
    }

    // ESC/POS GS ( k: URL ni QR kodga aylantiradi va printga yuborish uchun baytlar qaytaradi.
    private static byte[] BuildQrCode(string url)
    {
        var urlBytes = Encoding.ASCII.GetBytes(url);
        int dataLen = urlBytes.Length + 3; // cn(1) + fn(1) + m(1) + url bytes
        var pL = (byte)(dataLen & 0xFF);
        var pH = (byte)(dataLen >> 8);

        using var ms = new MemoryStream();
        ms.Write(QR_MODEL2);
        ms.Write(QR_SIZE7);
        ms.Write(QR_ERR_M);
        // Store data: GS ( k pL pH 31 50 30 [url]
        ms.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30]);
        ms.Write(urlBytes);
        ms.Write(QR_PRINT);
        return ms.ToArray();
    }
}
