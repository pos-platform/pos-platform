using PosCloud.Agent.Models;

namespace PosCloud.Agent.Devices;

/// <summary>
/// Chek printeri abstraksiyasi. Implementatsiyalar: <see cref="EscPosPrinter"/> (tarmoq TCP),
/// <see cref="UsbReceiptPrinter"/> (Windows winspool), <see cref="ConsoleReceiptPrinter"/> (test).
/// </summary>
public interface IReceiptPrinter
{
    Task PrintAsync(PendingReceipt receipt, FiscalResult fiscal, CancellationToken cancellationToken);

    Task PrintShiftReportAsync(ShiftReport report, CancellationToken cancellationToken);

    Task PrintRawAsync(byte[] data, CancellationToken cancellationToken);
}
