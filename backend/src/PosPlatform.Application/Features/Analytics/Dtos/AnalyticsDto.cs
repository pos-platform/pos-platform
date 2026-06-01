namespace PosPlatform.Application.Features.Analytics.Dtos;

public class AnalyticsDto
{
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal MarginPercent { get; set; }
    public int SalesCount { get; set; }
    public decimal AvgCheck { get; set; }

    public List<DailySalesDto> DailySales { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
}
