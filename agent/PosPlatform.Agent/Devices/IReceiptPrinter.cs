using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// Chek printeri abstraksiyasi. Keyinchalik ESC/POS (USB/tarmoq) implementatsiyasi qo'shiladi.
/// </summary>
public interface IReceiptPrinter
{
    Task PrintAsync(PendingReceipt receipt, FiscalResult fiscal, CancellationToken cancellationToken);

    Task PrintShiftReportAsync(ShiftReport report, CancellationToken cancellationToken);
}
