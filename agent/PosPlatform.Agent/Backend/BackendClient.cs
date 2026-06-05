using System.Net.Http.Json;
using PosPlatform.Agent.Authentication;
using PosPlatform.Agent.Models;

namespace PosPlatform.Agent.Backend;

/// <summary>Backendning fiskal endpointlari bilan ishlaydigan typed HttpClient.</summary>
public class BackendClient
{
    private readonly HttpClient _http;

    public BackendClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<PendingReceipt>> GetPendingAsync(int max, CancellationToken cancellationToken)
    {
        var result = await _http.GetFromJsonAsync<List<PendingReceipt>>(
            $"api/fiscal/pending?max={max}", cancellationToken);

        return result ?? new List<PendingReceipt>();
    }

    public async Task PostResultAsync(Guid saleId, FiscalResult result, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync($"api/fiscal/{saleId}/result", result, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ShiftReport>> GetPendingShiftReportsAsync(int max, CancellationToken cancellationToken)
    {
        var result = await _http.GetFromJsonAsync<List<ShiftReportRaw>>(
            $"api/shifts/pending-report?max={max}", cancellationToken);

        return result?.Select(r => new ShiftReport
        {
            ShiftId = r.Id,
            CashierName = r.CashierName,
            OpenedAt = r.OpenedAt,
            ClosedAt = r.ClosedAt,
            OpeningCash = r.OpeningCash,
            CountedCash = r.CountedCash,
            TotalSales = r.TotalSales,
            TotalRefunds = r.TotalRefunds,
            CashSalesNet = r.CashSalesNet,
            CardSalesNet = r.CardSalesNet,
            TransferSalesNet = r.TransferSalesNet,
            DebtSalesNet = r.DebtSalesNet,
            ExpectedCash = r.ExpectedCash,
            CashDifference = r.CashDifference,
            VatAmount = r.VatAmount,
            SaleCount = r.SaleCount,
            RefundCount = r.RefundCount,
        }).ToList() ?? new List<ShiftReport>();
    }

    public async Task MarkShiftReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsync($"api/shifts/{shiftId}/report-printed", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ShiftReport>> GetPendingXReportsAsync(int max, CancellationToken cancellationToken)
    {
        var result = await _http.GetFromJsonAsync<List<ShiftReportRaw>>(
            $"api/shifts/pending-x-report?max={max}", cancellationToken);

        return result?.Select(r => new ShiftReport
        {
            ShiftId = r.Id,
            CashierName = r.CashierName,
            OpenedAt = r.OpenedAt,
            ClosedAt = r.ClosedAt,
            OpeningCash = r.OpeningCash,
            CountedCash = r.CountedCash,
            TotalSales = r.TotalSales,
            TotalRefunds = r.TotalRefunds,
            CashSalesNet = r.CashSalesNet,
            CardSalesNet = r.CardSalesNet,
            TransferSalesNet = r.TransferSalesNet,
            DebtSalesNet = r.DebtSalesNet,
            ExpectedCash = r.ExpectedCash,
            CashDifference = r.CashDifference,
            VatAmount = r.VatAmount,
            SaleCount = r.SaleCount,
            RefundCount = r.RefundCount,
            IsXReport = true,
        }).ToList() ?? new List<ShiftReport>();
    }

    public async Task MarkXReportPrintedAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsync($"api/shifts/{shiftId}/x-report-printed", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // Backend CashShiftDto bilan mos raw mapping sinfi.
    private sealed class ShiftReportRaw
    {
        public Guid Id { get; set; }
        public string? CashierName { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal? CountedCash { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal CashSalesNet { get; set; }
        public decimal CardSalesNet { get; set; }
        public decimal TransferSalesNet { get; set; }
        public decimal DebtSalesNet { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CashDifference { get; set; }
        public decimal VatAmount { get; set; }
        public int SaleCount { get; set; }
        public int RefundCount { get; set; }
    }
}
