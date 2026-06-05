using Microsoft.Extensions.Options;
using PosPlatform.Agent.Backend;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Devices;

namespace PosPlatform.Agent.Workers;

/// <summary>
/// Yopilgan smenalar uchun X/Z hisobotni printer'ga chiqaradi.
/// Backend api/shifts/pending-report ni so'raydi, chop etib, report-printed deb belgilaydi.
/// </summary>
public class ShiftReportWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IReceiptPrinter _printer;
    private readonly AgentOptions _options;
    private readonly ILogger<ShiftReportWorker> _logger;

    public ShiftReportWorker(
        IServiceProvider services,
        IReceiptPrinter printer,
        IOptions<AgentOptions> options,
        ILogger<ShiftReportWorker> logger)
    {
        _services = services;
        _printer = printer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShiftReportWorker ishga tushdi.");

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
                _logger.LogWarning(ex, "Z-hisobot navbatini tekshirishda xatolik. Qayta urinamiz...");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var backend = scope.ServiceProvider.GetRequiredService<BackendClient>();

        var reports = await backend.GetPendingShiftReportsAsync(_options.BatchSize, cancellationToken);
        if (reports.Count == 0) return;

        _logger.LogInformation("{Count} ta smena Z-hisobot chop etish kutmoqda.", reports.Count);

        foreach (var report in reports)
        {
            try
            {
                await _printer.PrintShiftReportAsync(report, cancellationToken);
                await backend.MarkShiftReportPrintedAsync(report.ShiftId, cancellationToken);
                _logger.LogInformation("Z-hisobot chop etildi: smena {ShiftId}", report.ShiftId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Z-hisobot xatosi (smena {ShiftId}): {Msg}", report.ShiftId, ex.Message);
            }
        }
    }
}
