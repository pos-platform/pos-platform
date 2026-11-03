using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PosCloud.Agent.Configuration;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Devices;

/// <summary>
/// ESC/POS protokoli orqali termik printer (TCP raw socket, port 9100).
/// Sozlash: <c>Agent:PrinterMode = "Network"</c>, <c>Agent:PrinterAddress = "192.168.1.100:9100"</c>.
/// </summary>
public class EscPosPrinter : IReceiptPrinter
{
    private readonly AgentOptions _options;
    private readonly ILogger<EscPosPrinter> _logger;

    public EscPosPrinter(IOptions<AgentOptions> options, ILogger<EscPosPrinter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PrintAsync(PendingReceipt receipt, FiscalResult fiscal, CancellationToken cancellationToken)
    {
        if (!TryParseAddress(out var host, out var port))
        {
            _logger.LogWarning("PrinterAddress noto'g'ri format: {Addr}", _options.PrinterAddress);
            return;
        }

        var data = EscPosBuilder.Build(receipt, fiscal, _options.PrintWidth);

        try
        {
            using var client = new TcpClient();
            client.SendTimeout = _options.PrinterTimeoutMs;
            await client.ConnectAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            _logger.LogInformation("Chek chop etildi (Network): {ReceiptNumber} → {Addr} ({Bytes} bayt)",
                receipt.ReceiptNumber, _options.PrinterAddress, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer xatosi ({Addr}): {Msg}", _options.PrinterAddress, ex.Message);
        }
    }

    public async Task PrintShiftReportAsync(ShiftReport report, CancellationToken cancellationToken)
    {
        if (!TryParseAddress(out var host, out var port))
        {
            _logger.LogWarning("PrinterAddress noto'g'ri format: {Addr}", _options.PrinterAddress);
            return;
        }

        var data = EscPosBuilder.BuildShiftReport(report, _options.PrintWidth);

        try
        {
            using var client = new TcpClient();
            client.SendTimeout = _options.PrinterTimeoutMs;
            await client.ConnectAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            _logger.LogInformation("Z-hisobot chop etildi (Network): {Addr} ({Bytes} bayt)",
                _options.PrinterAddress, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer xatosi ({Addr}): {Msg}", _options.PrinterAddress, ex.Message);
        }
    }

    public async Task PrintRawAsync(byte[] data, CancellationToken cancellationToken)
    {
        if (!TryParseAddress(out var host, out var port)) return;
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = _options.PrinterTimeoutMs;
            await client.ConnectAsync(host, port, cancellationToken);
            await using var stream = client.GetStream();
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer raw xatosi: {Msg}", ex.Message);
        }
    }

    private bool TryParseAddress(out string host, out int port)
    {
        host = string.Empty;
        port = 9100;
        var addr = _options.PrinterAddress ?? string.Empty;
        var colon = addr.LastIndexOf(':');
        if (colon <= 0) return false;
        host = addr[..colon];
        return int.TryParse(addr[(colon + 1)..], out port);
    }
}
