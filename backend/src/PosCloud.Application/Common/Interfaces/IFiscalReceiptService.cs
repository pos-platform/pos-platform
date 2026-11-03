using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Chekni fiskalizatsiya qilish uchun kengaytirish nuqtasi (seam).
/// Hozir placeholder implementatsiya chekni NotSent holatida qoldiradi.
/// Keyinchalik bu interfeys lokal .NET agent (FDS / Rahmat MK) bilan aloqa
/// qiladigan haqiqiy implementatsiya bilan almashtiriladi.
/// </summary>
public interface IFiscalReceiptService
{
    /// <summary>
    /// Yakunlangan savdoni fiskalizatsiya navbatiga qo'yadi. Receipt holati
    /// shu yerda yangilanadi (masalan, Queued).
    /// </summary>
    Task EnqueueAsync(Sale sale, CancellationToken cancellationToken = default);
}
