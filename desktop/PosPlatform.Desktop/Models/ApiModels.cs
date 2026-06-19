using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public string BusinessType { get; set; } = "Store";
}

// ===================== Restoran modeli =====================

public class TableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Hall { get; set; }
    public int Seats { get; set; }
    public string Status { get; set; } = "Free";
    public Guid? OpenOrderId { get; set; }
    public decimal? OpenOrderTotal { get; set; }

    public bool IsOccupied => Status == "Occupied";
    public string SeatsLabel => $"{Seats} o'rin";
    public string TotalLabel => OpenOrderTotal.HasValue ? OpenOrderTotal.Value.ToString("N0") + " so'm" : "";
}

public class DishDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }

    public string PriceLabel => Price.ToString("N0");
    public string? ImageFull =>
        string.IsNullOrEmpty(ImageUrl) ? null : AppState.BaseUrl.TrimEnd('/') + ImageUrl;
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
    public bool SentToKitchen { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public Guid? TableId { get; set; }
    public int? GuestCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public string Status { get; set; } = "";
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OpenOrderRequest
{
    public Guid TableId { get; set; }
    public int? GuestCount { get; set; }
}

public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}

public class PayOrderRequest
{
    public string PaymentMethod { get; set; } = "Cash";
    public decimal PaidAmount { get; set; }
    public Guid? DebtorId { get; set; }
    public decimal ServiceChargePercent { get; set; }
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
    public bool IsWeighed { get; set; }
    /// <summary>Tarozi PLU raqami (vaznli mahsulot uchun). Tarozi shtrix-kodidagi kodga mos keladi.</summary>
    public int? PluNumber { get; set; }
    public Guid CategoryId { get; set; }

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

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
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
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "Sale";
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
    public ReceiptInfo? Receipt { get; set; }
}

public class SaleItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>Chekning fiskal holati (backend ReceiptDto bilan mos).</summary>
public class ReceiptInfo
{
    /// <summary>Backend enumni satr sifatida qaytaradi: NotSent, Queued, Registered, Failed.</summary>
    public string FiscalStatus { get; set; } = "";
    public string? FiscalSign { get; set; }
    public string? FiscalReceiptNumber { get; set; }
    public string? QrCodeUrl { get; set; }

    public bool IsRegistered => FiscalStatus == "Registered" && !string.IsNullOrEmpty(QrCodeUrl);
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
    public string TimeLabel => CreatedAt.ToLocalTime().ToString("HH:mm");
    public string TotalLabel => (Type == "Refund" ? "− " : "") + TotalAmount.ToString("N0");
    public string TypeLabel => Type == "Refund" ? "Qaytarish" : (Status == "Refunded" ? "Qaytarilgan" : "Sotuv");
    public string PaymentLabel => PaymentMethod switch
    {
        "Card" => "Karta",
        "Mixed" => "Aralash",
        "Debt" => "Nasiya",
        _ => "Naqd"
    };
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

    /// <summary>Savdoning serverdagi Id'si — fiskal holatni so'rab olish uchun (onlayn savdo).</summary>
    public Guid? SaleId { get; set; }

    /// <summary>Fiskal QR havola (agent fiskalizatsiya qilgach to'ladi).</summary>
    public string? QrCodeUrl { get; set; }
    /// <summary>Soliq qaytargan fiskal belgi (FP).</summary>
    public string? FiscalSign { get; set; }
    /// <summary>Soliq tizimidagi fiskal chek raqami.</summary>
    public string? FiscalReceiptNumber { get; set; }
}

public class ReceiptLine
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
}
