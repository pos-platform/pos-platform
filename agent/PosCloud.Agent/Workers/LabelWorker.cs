using Microsoft.Extensions.Options;
using PosCloud.Agent.Backend;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Devices;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Workers;

/// <summary>Narx yorliq chop etish: backenddan kutilayotgan vazifani oladi, printer'ga yuboradi.</summary>
public class LabelWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IReceiptPrinter _printer;
    private readonly AgentOptions _options;
    private readonly ILogger<LabelWorker> _logger;

    public LabelWorker(
        IServiceProvider services,
        IReceiptPrinter printer,
        IOptions<AgentOptions> options,
        ILogger<LabelWorker> logger)
    {
        _services = services;
        _printer = printer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LabelWorker ishga tushdi.");

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
                _logger.LogWarning(ex, "Yorliq navbatini tekshirishda xatolik. Qayta urinamiz...");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var backend = scope.ServiceProvider.GetRequiredService<BackendClient>();

        var job = await backend.GetPendingLabelJobAsync(cancellationToken);
        if (job is null) return;

        _logger.LogInformation("Yorliq vazifasi topildi: {JobId}, {Count} ta element.", job.Id, job.Items.Count);

        try
        {
            var labelItems = job.Items.Select(i => new LabelItem
            {
                ProductName = i.ProductName,
                Price = i.Price,
                Barcode = i.Barcode,
                Copies = i.Copies
            }).ToList();

            var bytes = EscPosBuilder.BuildLabels(labelItems, _options.PrintWidth);
            await _printer.PrintRawAsync(bytes, cancellationToken);
            await backend.MarkLabelJobPrintedAsync(job.Id, cancellationToken);
            _logger.LogInformation("Yorliq chop etildi: {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yorliq chop etishda xatolik (vazifa {JobId}): {Msg}", job.Id, ex.Message);
        }
    }
}
