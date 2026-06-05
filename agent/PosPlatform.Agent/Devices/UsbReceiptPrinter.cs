using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// USB termik printerga ESC/POS baytlarini to'g'ridan-to'g'ri yuboradi.
/// Windows print spooler orqali RAW datatype ishlatadi — printer drayverini talab qiladi.
/// Sozlash: <c>Agent:PrinterMode = "Usb"</c>, <c>Agent:PrinterName = "EPSON TM-T20 Receipt"</c>.
/// Epson TM-T20, BIXOLON SRP-350 va boshqa standart termik printerlarga mos.
/// </summary>
[SupportedOSPlatform("windows")]
public class UsbReceiptPrinter : IReceiptPrinter
{
    private readonly AgentOptions _options;
    private readonly ILogger<UsbReceiptPrinter> _logger;

    public UsbReceiptPrinter(IOptions<AgentOptions> options, ILogger<UsbReceiptPrinter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PrintAsync(PendingReceipt receipt, FiscalResult fiscal, CancellationToken cancellationToken)
    {
        var printerName = _options.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            _logger.LogWarning("PrinterName ko'rsatilmagan (USB printer uchun Agent:PrinterName ni o'rnating)");
            return Task.CompletedTask;
        }

        var data = EscPosBuilder.Build(receipt, fiscal);

        try
        {
            SendRaw(printerName, data);
            _logger.LogInformation("Chek chop etildi (USB): {ReceiptNumber} → \"{Printer}\" ({Bytes} bayt)",
                receipt.ReceiptNumber, printerName, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "USB printer xatosi (\"{Printer}\"): {Msg}", printerName, ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task PrintShiftReportAsync(ShiftReport report, CancellationToken cancellationToken)
    {
        var printerName = _options.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            _logger.LogWarning("PrinterName ko'rsatilmagan (USB printer uchun Agent:PrinterName ni o'rnating)");
            return Task.CompletedTask;
        }

        var data = EscPosBuilder.BuildShiftReport(report);

        try
        {
            SendRaw(printerName, data);
            _logger.LogInformation("Z-hisobot chop etildi (USB): \"{Printer}\" ({Bytes} bayt)",
                printerName, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "USB printer xatosi (\"{Printer}\"): {Msg}", printerName, ex.Message);
        }

        return Task.CompletedTask;
    }

    // ── Windows raw printer API (winspool.drv) ──────────────────────────────

    private static void SendRaw(string printerName, byte[] data)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Printer ochilmadi: \"{printerName}\" (Win32={Marshal.GetLastWin32Error()}). " +
                "Printer Devices & Printers da o'rnatilganligini tekshiring.");

        try
        {
            var di = new DocInfo { pDocName = "Chek", pDataType = "RAW" };
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
