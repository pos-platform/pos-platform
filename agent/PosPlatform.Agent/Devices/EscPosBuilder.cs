using System.Text;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

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

        Write(ESC_CENTER);
        Write(ESC_DOUBLE);
        Write(ESC_BOLD_ON);
        WriteLine("CHEK");
        Write(ESC_NORMAL);
        Write(ESC_BOLD_OFF);

        Write(ESC_CENTER);
        WriteLine($"No {receipt.ReceiptNumber}");
        WriteLine(receipt.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"));
        Write(ESC_LEFT);
        Divider();

        foreach (var item in receipt.Items)
        {
            var name = item.ProductName.Length > printWidth ? item.ProductName[..printWidth] : item.ProductName;
            WriteLine(name);
            var qtyLine = $"  {item.Quantity:0.###} x {item.UnitPrice:N2}";
            WriteLine(PadRight(qtyLine, item.LineTotal.ToString("N2"), printWidth));
        }

        Divider();

        Write(ESC_BOLD_ON);
        WriteLine(PadRight("JAMI:", $"{receipt.TotalAmount:N2} so'm", printWidth));
        Write(ESC_BOLD_OFF);
        WriteLine(PadRight("QQS (12%):", $"{receipt.VatAmount:N2}", printWidth));
        WriteLine(PadRight("To'lov:", receipt.PaymentMethod == "Cash" ? "Naqd" : "Karta", printWidth));

        Divider();

        Write(ESC_CENTER);
        WriteLine("*** FISKAL CHEK ***");
        WriteLine($"FP: {fiscal.FiscalSign}");
        if (!string.IsNullOrEmpty(fiscal.QrCodeUrl))
            WriteLine("QR: ofd.soliq.uz orqali tekshiring");

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

        // To'lov turlari (netto)
        Write(ESC_BOLD_ON);
        WriteLine("TO'LOV TURLARI (netto):");
        Write(ESC_BOLD_OFF);
        WriteLine(Row("  Naqd:", $"{report.CashSalesNet:N2}"));
        WriteLine(Row("  Karta:", $"{report.CardSalesNet:N2}"));
        WriteLine(Row("  O'tkazma:", $"{report.TransferSalesNet:N2}"));
        WriteLine(Row("  Nasiya:", $"{report.DebtSalesNet:N2}"));
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
}
