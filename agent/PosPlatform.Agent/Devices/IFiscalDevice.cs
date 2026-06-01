using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// Fiskal qurilma/xizmat abstraksiyasi (FDS / Rahmat MK). Keyinchalik haqiqiy
/// SDK/HTTP implementatsiyasi shu interfeysni almashtiradi.
/// </summary>
public interface IFiscalDevice
{
    Task<FiscalResult> FiscalizeAsync(PendingReceipt receipt, CancellationToken cancellationToken);
}
