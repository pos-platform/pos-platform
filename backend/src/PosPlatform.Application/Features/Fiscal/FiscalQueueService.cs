using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Fiscal.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Fiscal;

public class FiscalQueueService : IFiscalQueueService
{
    private readonly IApplicationDbContext _db;

    /// <summary>
    /// Vaqtinchalik xato bo'lganda chek necha marta avto-qayta urinilishi. Chegaraga yetgach
    /// Failed bo'ladi (5s poll oralig'ida ~30 urinish ≈ 2.5 daqiqa vaqtinchalik uzilishni qoplaydi).
    /// </summary>
    private const int MaxFiscalRetries = 30;

    public FiscalQueueService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PendingReceiptDto>> GetPendingAsync(int max, string? terminalId = null, CancellationToken cancellationToken = default)
    {
        max = Math.Clamp(max, 1, 100);
        terminalId = string.IsNullOrWhiteSpace(terminalId) ? null : terminalId.Trim();

        var query = _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
            .Where(s => s.Receipt != null && s.Receipt.FiscalStatus == FiscalStatus.Queued);

        // terminalId berilsa — faqat shu stantsiyaning cheklari (har terminalda o'z moduli/agenti).
        // Bo'sh bo'lsa — barcha cheklar (bitta agentli eski rejim, ortga moslik).
        if (terminalId is not null)
            query = query.Where(s => s.TerminalId == terminalId);

        var sales = await query
            .OrderBy(s => s.CreatedAt)
            .Take(max)
            .ToListAsync(cancellationToken);

        return sales.Select(PendingReceiptDto.FromEntity).ToList();
    }

    public async Task ApplyResultAsync(Guid saleId, FiscalResultDto result, CancellationToken cancellationToken = default)
    {
        var receipt = await _db.Receipts
            .FirstOrDefaultAsync(r => r.SaleId == saleId, cancellationToken);

        if (receipt is null)
        {
            throw new NotFoundException($"Chek topilmadi (SaleId): {saleId}");
        }

        if (result.Success)
        {
            receipt.FiscalStatus = FiscalStatus.Registered;
            receipt.FiscalSign = result.FiscalSign;
            receipt.FiscalModuleId = result.FiscalModuleId;
            receipt.FiscalRegistrationNumber = result.FiscalRegistrationNumber;
            receipt.FiscalReceiptNumber = result.FiscalReceiptNumber;
            receipt.QrCodeUrl = result.QrCodeUrl;
            receipt.FiscalizedAt = result.FiscalizedAt ?? DateTime.UtcNow;
            receipt.ErrorMessage = null;
            receipt.FiscalRetryCount = 0;
        }
        else
        {
            // Xato matnini har doim saqlaymiz (audit ko'rinishi uchun).
            receipt.ErrorMessage = result.ErrorMessage;

            // Vaqtinchalik (aniq ro'yxatga OLINMAGAN, o'z-o'zidan tuzaladigan) xato — chekni
            // navbatga qaytarib avtomatik qayta urinamiz (modul qaytib ulanadi, soat tuzaladi).
            // Doimiy yoki noaniq (double-reg xavfi bor) xato — Failed qoldirib qo'lda ko'riladi.
            if (IsTransient(result.ErrorMessage) && receipt.FiscalRetryCount < MaxFiscalRetries)
            {
                receipt.FiscalRetryCount++;
                receipt.FiscalStatus = FiscalStatus.Queued;
            }
            else
            {
                receipt.FiscalStatus = FiscalStatus.Failed;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Xato o'z-o'zidan tuzaladigan va chek modulda ANIQ ro'yxatga olinmagan (qayta urinish
    /// xavfsiz — double-registratsiya xavfi yo'q) turdami. Faqat aniq belgilangan xatolar
    /// vaqtinchalik hisoblanadi; notanish yoki noaniq (timeout — modul ro'yxatga olgan bo'lishi
    /// mumkin) xatolar Failed bo'ladi.
    /// </summary>
    private static bool IsTransient(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return false;

        // Barchasi: (a) modulga umuman yetib bormagan yoki (b) applet chekни QABUL QILMAGAN
        // (SWError) — ya'ni fiskal belgi berilmagan, shuning uchun qayta urinish xavfsiz.
        string[] transientMarkers =
        {
            "FiscalDrive/List",           // modul topilmadi (ulanmagan/drayver ko'rmayapti)
            "modul topilmadi",
            "DATETIME_IS_IN_THE_PAST",    // kompyuter soati orqada — soat tuzalgach o'tadi
            "SEND_ALL_RECEIPTS_FIRST",    // modul to'lgan — OFD sinxronidan keyin o'tadi
            "LOCKED_SYNC_WITH_SERVER",
            "(GetTXID)",                  // ro'yxatga olishdan OLDINGI bosqich — qayta urinish xavfsiz
            "TXID qaytarmadi",
            "actively refused",           // drayver ishlamayapti (WinSock 10061)
            "Connection refused",
            "No connection could be made"
        };

        return transientMarkers.Any(m => error.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
