using System.Text;
using Microsoft.Extensions.Logging;
using PosCloud.Agent.Models;

namespace PosCloud.Agent.Devices;

/// <summary>
/// Chekni konsolga "chop etadigan" test implementatsiyasi (PrinterMode = "Console").
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

    public Task PrintShiftReportAsync(ShiftReport report, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(report.IsXReport ? "======= SMENA X-HISOBOT =======" : "======= SMENA Z-HISOBOT =======");
        sb.AppendLine($"  Kassir:    {report.CashierName}");
        sb.AppendLine($"  Ochildi:   {report.OpenedAt.ToLocalTime():dd.MM.yyyy HH:mm}");
        sb.AppendLine($"  Yopildi:   {report.ClosedAt?.ToLocalTime():dd.MM.yyyy HH:mm}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"  Sotuvlar:  {report.SaleCount} ta  ({report.TotalSales:N2})");
        sb.AppendLine($"  Qaytarish: {report.RefundCount} ta  ({report.TotalRefunds:N2})");
        sb.AppendLine("--- To'lov turlari (netto) -----");
        sb.AppendLine($"  Naqd:      {report.CashSalesNet:N2}");
        sb.AppendLine($"  Karta:     {report.CardSalesNet:N2}");
        sb.AppendLine($"  O'tkazma:  {report.TransferSalesNet:N2}");
        sb.AppendLine($"  Nasiya:    {report.DebtSalesNet:N2}");
        sb.AppendLine("--- Kassa -------------------");
        sb.AppendLine($"  Boshlangich: {report.OpeningCash:N2}");
        sb.AppendLine($"  Kutilgan:    {report.ExpectedCash:N2}");
        sb.AppendLine($"  Sanab:       {report.CountedCash:N2}");
        sb.AppendLine($"  Farq:        {report.CashDifference:N2}");
        sb.AppendLine($"  QQS:         {report.VatAmount:N2}");
        sb.AppendLine("================================");

        _logger.LogInformation("Z-hisobot chop etildi:{Report}", sb.ToString());
        return Task.CompletedTask;
    }

    public Task PrintRawAsync(byte[] data, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Raw chop etildi: {Bytes} bayt", data.Length);
        return Task.CompletedTask;
    }
}
