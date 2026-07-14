using PosPlatform.Desktop.Services;

namespace PosPlatform.Desktop.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }
    public string Role { get; set; } = "";
    public string BusinessType { get; set; } = "Store";

    /// <summary>Tashkilot nomi.</summary>
    public string? OrganizationName { get; set; }

    /// <summary>Tashkilot STIR (ИНН) — fiskal chek sarlavhasida chiqadi.</summary>
    public string? OrganizationStir { get; set; }

    /// <summary>Biriktirilgan do'kon (filial) nomi — chek sarlavhasida ishlatiladi.</summary>
    public string? BranchName { get; set; }

    /// <summary>Filial manzili — fiskal chek sarlavhasida chiqadi.</summary>
    public string? BranchAddress { get; set; }

    /// <summary>Obuna shu sanagacha amal qiladi (oylik to'lov). null = faollashtirilmagan.</summary>
    public DateTime? LicensedUntil { get; set; }

    /// <summary>Doimiy (bir martalik to'liq sotib olingan) litsenziya — obuna hech qachon tugamaydi.</summary>
    public bool IsLifetime { get; set; }
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

    /// <summary>QQS (НДС) bilanmi — fiskal chekda satr QQSi hisoblanadi.</summary>
    public bool IsVat { get; set; }
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
    /// <summary>Kassa stantsiyasi ID (masalan "KASSA-1") — fiskal agent shu bo'yicha filtrlaydi.</summary>
    public string? TerminalId { get; set; }
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
    public string? FiscalModuleId { get; set; }
    public string? FiscalRegistrationNumber { get; set; }
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

    /// <summary>Chek sarlavhasidagi tashkilot nomi. Bo'sh bo'lsa "PosPlatform" ishlatiladi.</summary>
    public string StoreName { get; set; } = "";

    /// <summary>Tashkilot STIR (ИНН) — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? Stir { get; set; }

    /// <summary>Filial manzili — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? Address { get; set; }

    /// <summary>Kassa raqami (TerminalId) — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? Kassa { get; set; }

    /// <summary>Fiskal modul qaytargan smena raqami (ixtiyoriy).</summary>
    public string? FiscalShiftNumber { get; set; }

    public string Cashier { get; set; } = "";
    public string Payment { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Change { get; set; }
    public List<ReceiptLine> Lines { get; set; } = new();

    /// <summary>QQS (НДС) jami — narxga kiritilgan 12% (faqat QQSli satrlar). 0 bo'lsa ko'rsatilmaydi.</summary>
    public decimal VatTotal => Lines.Where(l => l.IsVat).Sum(l => Math.Round(l.LineTotal * 12m / 112m, 2));

    /// <summary>Savdoning serverdagi Id'si — fiskal holatni so'rab olish uchun (onlayn savdo).</summary>
    public Guid? SaleId { get; set; }

    /// <summary>Fiskal QR havola (agent fiskalizatsiya qilgach to'ladi).</summary>
    public string? QrCodeUrl { get; set; }
    /// <summary>Soliq qaytargan fiskal belgi (FB).</summary>
    public string? FiscalSign { get; set; }
    /// <summary>Fiskal modul identifikatori (FM).</summary>
    public string? FiscalModuleId { get; set; }
    /// <summary>Kassa apparatining ro'yxatga olish raqami (RS).</summary>
    public string? FiscalRegistrationNumber { get; set; }
    /// <summary>Soliq tizimidagi fiskal chek raqami.</summary>
    public string? FiscalReceiptNumber { get; set; }
}

public class ReceiptLine
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>MXIK (ИКПУ) kodi — fiskal chekda mahsulot ostida chiqadi.</summary>
    public string MxikCode { get; set; } = "";

    /// <summary>Shtrix-kod (ШК) — fiskal chekda mahsulot ostida chiqadi (ixtiyoriy).</summary>
    public string? Barcode { get; set; }

    /// <summary>QQS (НДС) bilanmi — narxga kiritilgan 12%.</summary>
    public bool IsVat { get; set; }
}
