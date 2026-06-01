using System.Text;
using Microsoft.Extensions.Logging;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Devices;

/// <summary>
/// Chekni konsolga "chop etadigan" placeholder. Haqiqiy ESC/POS printer
/// implementatsiyasi shu interfeysni almashtiradi.
/// </summary>
public class ConsoleReceiptPrinter : IReceiptPrinter
{
    private readonly ILogger<ConsoleReceiptPrinter> _logger;

    public ConsoleReceiptPrinter(ILogger<ConsoleReceiptPrinter> logger)
    {
        _logger = logger;
    }

    public Task PrintAsync(PendingReceipt receipt, FiscalResult fiscal, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("----------- CHEK -----------");
        sb.AppendLine($"  № {receipt.ReceiptNumber}");
        sb.AppendLine($"  {receipt.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine("----------------------------");
        foreach (var item in receipt.Items)
        {
            sb.AppendLine($"  {item.ProductName}");
            sb.AppendLine($"    {item.Quantity} x {item.UnitPrice:N2} = {item.LineTotal:N2}");
            sb.AppendLine($"    MXIK: {item.MxikCode}");
        }
        sb.AppendLine("----------------------------");
        sb.AppendLine($"  JAMI:  {receipt.TotalAmount:N2}");
        sb.AppendLine($"  QQS:   {receipt.VatAmount:N2}");
        sb.AppendLine($"  FP:    {fiscal.FiscalSign}");
        sb.AppendLine($"  QR:    {fiscal.QrCodeUrl}");
        sb.AppendLine("----------------------------");

        _logger.LogInformation("Chek chop etildi:{Receipt}", sb.ToString());
        return Task.CompletedTask;
    }
}
