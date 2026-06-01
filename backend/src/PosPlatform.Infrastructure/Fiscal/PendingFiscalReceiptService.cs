using Microsoft.Extensions.Logging;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Infrastructure.Fiscal;

/// <summary>
/// IFiscalReceiptService implementatsiyasi: chekni fiskal navbatga (Queued)
/// qo'yadi. Haqiqiy fiskalizatsiyani lokal .NET agent (FDS / Rahmat MK) bajaradi —
/// u backenddan kutayotgan cheklarni "tortib oladi" (IFiscalQueueService orqali)
/// va natijani qaytaradi. Push o'rniga pull ishlatamiz, chunki agent lokalda
/// (NAT ortida) turadi.
/// </summary>
public class PendingFiscalReceiptService : IFiscalReceiptService
{
    private readonly ILogger<PendingFiscalReceiptService> _logger;

    public PendingFiscalReceiptService(ILogger<PendingFiscalReceiptService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        if (sale.Receipt is not null)
        {
            sale.Receipt.FiscalStatus = FiscalStatus.Queued;
        }

        _logger.LogInformation(
            "Savdo {ReceiptNumber} fiskal navbatga qo'yildi (Queued).",
            sale.ReceiptNumber);

        return Task.CompletedTask;
    }
}
