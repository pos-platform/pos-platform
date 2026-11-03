using Microsoft.Extensions.Options;
using PosCloud.Agent.Backend;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Devices;
using PosCloud.Agent.Models;
using PosCloud.Agent.Services;

namespace PosCloud.Agent.Workers;

/// <summary>
/// Asosiy halqa: backenddan kutayotgan cheklarni davriy "tortib oladi",
/// har birini fiskalizatsiya qiladi, chop etadi va natijani qaytaradi.
/// </summary>
public class FiscalWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IFiscalDevice _fiscalDevice;
    private readonly IReceiptPrinter _printer;
    private readonly FiscalLedger _ledger;
    private readonly AgentOptions _options;
    private readonly ILogger<FiscalWorker> _logger;

    public FiscalWorker(
        IServiceProvider services,
        IFiscalDevice fiscalDevice,
        IReceiptPrinter printer,
        FiscalLedger ledger,
        IOptions<AgentOptions> options,
        ILogger<FiscalWorker> logger)
    {
        _services = services;
        _fiscalDevice = fiscalDevice;
        _printer = printer;
        _ledger = ledger;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Fiskal agent ishga tushdi. Backend: {Url}, oraliq: {Interval}s",
            _options.BackendBaseUrl, _options.PollIntervalSeconds);

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Backend o'chiq yoki tarmoq xatosi — keyingi tsiklda qayta urinamiz.
                _logger.LogWarning(ex, "Navbatni tekshirishda xatolik. Qayta urinamiz...");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        // BackendClient typed HttpClient — har tsiklda scope'dan olamiz.
        using var scope = _services.CreateScope();
        var backend = scope.ServiceProvider.GetRequiredService<BackendClient>();

        var pending = await backend.GetPendingAsync(_options.BatchSize, _options.FiscalTerminalId, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogInformation("{Count} ta chek fiskalizatsiya kutmoqda.", pending.Count);

        foreach (var receipt in pending)
        {
            // ── Idempotentlik qalqoni ────────────────────────────────────────
            // Chek modulda allaqachon ro'yxatdan o'tgan bo'lsa (oldingi tsiklda muvaffaqiyatli
            // bo'lib, natija backendga yetib bormagan) — modulga QAYTA YUBORMAYMIZ, aks holda
            // soliqqa dublikat haqiqiy chek ketadi. Saqlangan natijani qayta yuboramiz.
            if (_ledger.TryGet(receipt.SaleId, out var known))
            {
                _logger.LogWarning(
                    "Chek {Receipt} allaqachon fiskalizatsiya qilingan (FP={Sign}) — "
                    + "modulga qayta yuborilmaydi, natija backendga qayta yuborilmoqda.",
                    receipt.ReceiptNumber, known.FiscalSign);

                await backend.PostResultAsync(receipt.SaleId, known, cancellationToken);
                continue;
            }

            FiscalResult result;
            try
            {
                result = await _fiscalDevice.FiscalizeAsync(receipt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiskalizatsiya xatosi: {Receipt}", receipt.ReceiptNumber);
                result = new FiscalResult { Success = false, ErrorMessage = ex.Message };
            }

            if (result.Success)
            {
                // Backendga yuborishdan OLDIN diskka yozamiz — yuborish yiqilsa ham
                // keyingi tsiklda takroriy fiskalizatsiya bo'lmasligi uchun.
                await _ledger.RecordAsync(receipt.SaleId, result, cancellationToken);
                await _printer.PrintAsync(receipt, result, cancellationToken);
            }

            await backend.PostResultAsync(receipt.SaleId, result, cancellationToken);

            _logger.LogInformation(
                "Chek {Receipt}: {Status}",
                receipt.ReceiptNumber, result.Success ? "fiskalizatsiya qilindi" : "xatolik");
        }
    }
}
