namespace PosPlatform.Agent.Models;

/// <summary>Backenddan keladigan smena X/Z hisobot ma'lumotlari.</summary>
public class ShiftReport
{
    public Guid ShiftId { get; set; }
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
