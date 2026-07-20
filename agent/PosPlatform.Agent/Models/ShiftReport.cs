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

    /// <summary>To'lov turlari kesimi — har biri QR kodli/kodsiz ajratilgan.</summary>
    public List<ShiftPaymentLine> Payments { get; set; } = new();

    public int FiscalSaleCount { get; set; }
    public decimal FiscalSalesTotal { get; set; }
    public int NonFiscalSaleCount { get; set; }
    public decimal NonFiscalSalesTotal { get; set; }

    /// <summary>true = X-hisobot (ochiq smena); false = Z-hisobot (yopilgan smena).</summary>
    public bool IsXReport { get; set; }
}

/// <summary>Smenadagi bitta to'lov turi (soliq QR kodi olingan/olinmagan qismlarga ajratilgan).</summary>
public class ShiftPaymentLine
{
    /// <summary>Cash, Card, Transfer, Debt, Mixed.</summary>
    public string Method { get; set; } = string.Empty;

    public int SaleCount { get; set; }
    public decimal SaleAmount { get; set; }
    public int RefundCount { get; set; }
    public decimal RefundAmount { get; set; }

    public int FiscalCount { get; set; }
    public decimal FiscalAmount { get; set; }
    public int NonFiscalCount { get; set; }
    public decimal NonFiscalAmount { get; set; }

    public decimal NetAmount => SaleAmount - RefundAmount;

    public string MethodLabel => Method switch
    {
        "Card" => "Karta",
        "Transfer" => "O'tkazma",
        "Mixed" => "Aralash",
        "Debt" => "Nasiya",
        _ => "Naqd"
    };
}
