using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Reports.Dtos;

// Bosqich 10.3: sotilgan tovarlar logi (SaleItem asosida).

public class SoldItemsQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CashierId { get; set; }
    public Guid? CustomerId { get; set; }

    /// <summary>Pos | Online | All (standart).</summary>
    public string? Source { get; set; }

    /// <summary>Sale | Refund | All (standart).</summary>
    public string? Type { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SoldItemDto
{
    public Guid SaleId { get; set; }
    public Guid SaleItemId { get; set; }
    public DateTime SoldAt { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public SaleType Type { get; set; }

    /// <summary>"Pos" yoki "Online".</summary>
    public string Source { get; set; } = "Pos";
    public string? BranchName { get; set; }
    public string? CashierName { get; set; }
    public string? CustomerName { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal RefundedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CostPrice { get; set; }

    /// <summary>LineTotal − CostPrice × Quantity (refund qatorida manfiy ishora bilan).</summary>
    public decimal Profit { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Savdo keshbeki qatorlarga summasi bo'yicha taqsimlangan ulush (refundda — ayirilgan keshbek, manfiy).</summary>
    public decimal CashbackEarned { get; set; }
    public FiscalStatus? FiscalStatus { get; set; }
    public int? OnlineOrderNumber { get; set; }
}

public class SoldItemsTotalsDto
{
    /// <summary>Sotilgan − qaytarilgan miqdor.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Sotuv qatorlari − qaytarish qatorlari summasi.</summary>
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }

    /// <summary>Qaytarish qatorlari summasi.</summary>
    public decimal Refunds { get; set; }
}

public class SoldItemsPageDto
{
    public List<SoldItemDto> Items { get; set; } = [];
    public int Total { get; set; }
    public SoldItemsTotalsDto Totals { get; set; } = new();
}
