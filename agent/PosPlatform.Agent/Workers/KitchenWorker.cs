using Microsoft.Extensions.Options;
using PosPlatform.Agent.Backend;
using PosPlatform.Agent.Configuration;
using PosPlatform.Agent.Devices;

namespace PosPlatform.Agent.Workers;

/// <summary>Oshxona cheki chop etish: backenddan kutilayotgan oshxona chekini oladi, printerga yuboradi.</summary>
public class KitchenWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IReceiptPrinter _printer;
    private readonly AgentOptions _options;
    private readonly ILogger<KitchenWorker> _logger;

    public KitchenWorker(
        IServiceProvider services,
        IReceiptPrinter printer,
        IOptions<AgentOptions> options,
        ILogger<KitchenWorker> logger)
    {
        _services = services;
        _printer = printer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KitchenWorker ishga tushdi.");

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
                _logger.LogWarning(ex, "Oshxona navbatini tekshirishda xatolik. Qayta urinamiz...");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var backend = scope.ServiceProvider.GetRequiredService<BackendClient>();

        var ticket = await backend.GetPendingKitchenTicketAsync(cancellationToken);
        if (ticket is null) return;

        _logger.LogInformation("Oshxona cheki topildi: {TicketId} (stol {Table}), {Count} ta element.",
            ticket.Id, ticket.TableName, ticket.Items.Count);

        try
        {
            var bytes = EscPosBuilder.BuildKitchenTicket(ticket, _options.PrintWidth);
            await _printer.PrintRawAsync(bytes, cancellationToken);
            await backend.MarkKitchenTicketPrintedAsync(ticket.Id, cancellationToken);
            _logger.LogInformation("Oshxona cheki chop etildi: {TicketId}", ticket.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oshxona cheki chop etishda xatolik (chek {TicketId}): {Msg}", ticket.Id, ex.Message);
        }
    }
}
