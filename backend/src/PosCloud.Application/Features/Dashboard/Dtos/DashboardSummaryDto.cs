namespace PosCloud.Application.Features.Dashboard.Dtos;

public class DashboardSummaryDto
{
    /// <summary>Jami sotuv (Sale turidagi savdolar summasi).</summary>
    public decimal TotalSales { get; set; }

    /// <summary>Naqd kirim (naqd sotuv − naqd qaytarish).</summary>
    public decimal CashIncome { get; set; }

    /// <summary>Karta kirim (karta sotuv − karta qaytarish).</summary>
    public decimal CardIncome { get; set; }

    /// <summary>Qaytarishlar (Refund turidagi savdolar summasi).</summary>
    public decimal Returns { get; set; }

    /// <summary>Joriy umumiy qarz (barcha qarzdorlar balansi).</summary>
    public decimal Debts { get; set; }

    /// <summary>Davr xarajatlari.</summary>
    public decimal Expenses { get; set; }

    /// <summary>Savdolar soni.</summary>
    public int SalesCount { get; set; }
}
