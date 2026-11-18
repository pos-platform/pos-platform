namespace PosCloud.Application.Features.Reports.Dtos;

public class SalesReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalCount { get; set; }
    /// <summary>Sotuvlar summasi (qaytarishlarsiz).</summary>
    public decimal GrossRevenue { get; set; }
    /// <summary>Sof tushum = sotuvlar − qaytarishlar.</summary>
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public List<SalesReportDayDto> Days { get; set; } = new();
    public List<SalesReportProductDto> TopProducts { get; set; } = new();
}

public class SalesReportDayDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class SalesReportProductDto
{
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class StockReportDto
{
    public int TotalProducts { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public List<StockReportItemDto> Items { get; set; } = new();
}

public class StockReportItemDto
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Barcode { get; set; }
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostValue { get; set; }
    public decimal SaleValue { get; set; }
}
