using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Barcode { get; set; }
    public string MxikCode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Piece";

    public string? ImageUrl { get; set; }

    public string UnitLabel => Unit == "Kilogram" ? "kg" : "dona";
    public string PriceLabel => Price.ToString("N0");
    public string QtyLabel => Quantity.ToString("N0");

    /// <summary>Backenddagi nisbiy rasm yo'lini to'liq URL'ga aylantiradi.</summary>
    public string? ImageFull =>
        string.IsNullOrEmpty(ImageUrl) ? null : AppState.BaseUrl.TrimEnd('/') + ImageUrl;
}

public class DebtorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }

    public string Display => $"{Name}  (qarz: {Balance:N0})";
}

public class ShiftDto
{
    public Guid Id { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime OpenedAt { get; set; }
    public string Status { get; set; } = "";
}

public class SaleItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateSaleRequest
{
    public string PaymentMethod { get; set; } = "Cash";
    public decimal PaidAmount { get; set; }
    public Guid? DebtorId { get; set; }
    public List<SaleItemRequest> Items { get; set; } = new();
}

public class SaleDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal ChangeAmount { get; set; }
}

public class SaleListDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string Type { get; set; } = "Sale";
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";

    public string DateLabel => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string TotalLabel => TotalAmount.ToString("N0");
    public string TypeLabel => Type == "Refund" ? "Qaytarish" : (Status == "Refunded" ? "Qaytarilgan" : "Sotuv");
    public bool CanRefund => Type == "Sale" && Status != "Refunded";
}

/// <summary>Chek chop etish uchun lokal ma'lumot (sotuv paytida yig'iladi).</summary>
public class ReceiptData
{
    public string ReceiptNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public string Cashier { get; set; } = "";
    public string Payment { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Change { get; set; }
    public List<ReceiptLine> Lines { get; set; } = new();
}

public class ReceiptLine
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
}
