using Microsoft.Extensions.Options;
using PosCloud.Agent.Backend;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Devices;

namespace PosCloud.Agent.Workers;

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

        var zReports = await backend.GetPendingShiftReportsAsync(_options.BatchSize, cancellationToken);
        if (zReports.Count > 0)
        {
            _logger.LogInformation("{Count} ta smena Z-hisobot chop etish kutmoqda.", zReports.Count);
            foreach (var report in zReports)
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

        var xReports = await backend.GetPendingXReportsAsync(_options.BatchSize, cancellationToken);
        if (xReports.Count > 0)
        {
            _logger.LogInformation("{Count} ta smena X-hisobot chop etish kutmoqda.", xReports.Count);
            foreach (var report in xReports)
            {
                try
                {
                    await _printer.PrintShiftReportAsync(report, cancellationToken);
                    await backend.MarkXReportPrintedAsync(report.ShiftId, cancellationToken);
                    _logger.LogInformation("X-hisobot chop etildi: smena {ShiftId}", report.ShiftId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "X-hisobot xatosi (smena {ShiftId}): {Msg}", report.ShiftId, ex.Message);
                }
            }
        }
    }
}
