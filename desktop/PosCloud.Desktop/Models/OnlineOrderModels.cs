namespace PosCloud.Desktop.Models;

/// <summary>Online (mobil ilova) buyurtma — /api/online-orders. Enum'lar serverdan string bo'lib keladi.</summary>
public class OnlineOrderDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Status { get; set; } = "";
    public string FulfillmentType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public bool Contactless { get; set; }
    public string? Comment { get; set; }
    public string OutOfStockPolicy { get; set; } = "";
    public decimal ItemsTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TransferProofUrl { get; set; }
    public Guid? SaleId { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? CancelReason { get; set; }
    public string? CourierName { get; set; }
    public string? CourierPhone { get; set; }
    public DateTime? PickingStartedAt { get; set; }

    // ---- Bosqich 6: kuryer, promokod, bonus ----
    public Guid? CourierUserId { get; set; }
    /// <summary>Kuryer mijozdan olgan naqd (kuryer ilovasida "Yetkazildi"da yoziladi).</summary>
    public decimal? CourierCashCollected { get; set; }
    /// <summary>Kuryer naqdni kassaga topshirgan vaqt (null — hali topshirilmagan).</summary>
    public DateTime? CourierCashHandedOverAt { get; set; }
    public string? PromoCode { get; set; }
    public decimal PromoDiscount { get; set; }
    public decimal BonusUsed { get; set; }

    public List<OnlineOrderItemDto> Items { get; set; } = new();

    // ---- ko'rinish uchun ----
    public bool IsDelivery => FulfillmentType == "Delivery";
    public bool IsPaid => PaymentStatus == "Paid";
    public int ActiveItemCount => Items.Count(i => !i.IsRemoved);

    public string Title => $"#{Number}";
    public string TypeText => IsDelivery ? "Yetkazish" : "Olib ketish";
    public string StatusText => OnlineOrderTexts.Status(Status);

    public string PaymentText
    {
        get
        {
            var method = PaymentMethod switch
            {
                "Cash" => "Naqd",
                "Card" => "Terminal",
                "Transfer" => "O'tkazma",
                _ => PaymentMethod
            };
            return IsPaid ? $"{method} · to'langan" : method;
        }
    }

    public string CustomerLine => $"{CustomerName} · {MaskedPhone} · {ActiveItemCount} xil";

    /// <summary>Kartochka uchun yashirilgan telefon: +998 90 *** ** 67. To'liq raqam faqat tafsilot panelida.</summary>
    public string MaskedPhone => OnlineOrderTexts.MaskPhone(CustomerPhone);

    public bool HasCourier => !string.IsNullOrWhiteSpace(CourierName) || !string.IsNullOrWhiteSpace(CourierPhone);

    /// <summary>"Kuryer: Bekzod · +998 ..." yoki bo'sh.</summary>
    public string CourierText => HasCourier
        ? "Kuryer: " + string.Join(" · ", new[] { CourierName, CourierPhone }.Where(x => !string.IsNullOrWhiteSpace(x)))
        : "";

    public System.Windows.Visibility CourierVisibility =>
        HasCourier ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    public string TotalText => $"{TotalAmount:N0} so'm";

    public string AgeText
    {
        get
        {
            var minutes = (int)(DateTime.UtcNow - CreatedAt.ToUniversalTime()).TotalMinutes;
            var time = ScheduledFor is { } s ? $" · {s.ToLocalTime():HH:mm} ga" : "";
            return (minutes < 1 ? "hozir" : minutes < 60 ? $"{minutes} daq oldin" : $"{CreatedAt.ToLocalTime():HH:mm}") + time;
        }
    }

    /// <summary>Yangi buyurtma 5 daqiqadan ko'p kutsa — qizil.</summary>
    public bool IsLate => Status == "New" && (DateTime.UtcNow - CreatedAt.ToUniversalTime()).TotalMinutes >= 5;

    public string AddressText
    {
        get
        {
            if (!IsDelivery) return "Olib ketish";
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(AddressLine)) parts.Add(AddressLine!);
            if (!string.IsNullOrWhiteSpace(Entrance)) parts.Add($"{Entrance}-podyezd");
            if (!string.IsNullOrWhiteSpace(Floor)) parts.Add($"{Floor}-qavat");
            if (!string.IsNullOrWhiteSpace(Apartment)) parts.Add($"{Apartment}-xonadon");
            if (!string.IsNullOrWhiteSpace(Intercom)) parts.Add($"domofon {Intercom}");
            if (!string.IsNullOrWhiteSpace(Landmark)) parts.Add($"mo'ljal: {Landmark}");
            return string.Join(", ", parts);
        }
    }

    public string PolicyText => OutOfStockPolicy switch
    {
        "ReplaceSimilar" => "Tugasa: o'xshashiga almashtirish",
        "Remove" => "Tugasa: olib tashlash",
        _ => "Tugasa: mijozga qo'ng'iroq"
    };
}

public class OnlineOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Barcode { get; set; }
    public bool IsWeighed { get; set; }
    public bool IsMarked { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PickedQuantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsRemoved { get; set; }
    public Guid? ReplacesItemId { get; set; }
    public decimal StockQuantity { get; set; }
    /// <summary>Markirovka (DataMatrix) kodlari — markirovkali tovarda soni = butun miqdor.</summary>
    public List<string> MarkingCodes { get; set; } = new();

    public decimal EffectiveQuantity => IsRemoved ? 0 : PickedQuantity ?? Quantity;

    /// <summary>Kerakli markirovka kodlari soni (markirovkasiz yoki olib tashlangan bo'lsa 0).</summary>
    public int RequiredMarkingCodes => IsMarked && !IsRemoved ? (int)Math.Ceiling(EffectiveQuantity) : 0;
    public bool MarkingComplete => MarkingCodes.Count >= RequiredMarkingCodes;
    public string MarkingText => IsMarked && !IsRemoved ? $"Markirovka: {MarkingCodes.Count} / {RequiredMarkingCodes}" : "";
    public System.Windows.Visibility MarkingVisibility =>
        IsMarked && !IsRemoved ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    public System.Windows.Media.Brush MarkingBrush => MarkingComplete
        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0B, 0x6B, 0x45))
        : System.Windows.Media.Brushes.Firebrick;
    public string QuantityText => IsRemoved ? "olib tashlandi" : $"× {EffectiveQuantity:0.###}";
    public string CodeText => $"{Barcode ?? "—"} · qoldiq {StockQuantity:0.###}";
}

public class OnlineStoreSettingsDto
{
    public bool IsEnabled { get; set; }
    public bool IsAcceptingOrders { get; set; }
    public bool IsVisible { get; set; }
}

/// <summary>GET /api/online-orders/couriers elementi — filialning faol kuryeri.</summary>
public class OnlineCourierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public int ActiveOrders { get; set; }

    public string DisplayText => Name +
                                 (string.IsNullOrWhiteSpace(Phone) ? "" : $" · {Phone}") +
                                 (ActiveOrders > 0 ? $" · faol buyurtma: {ActiveOrders}" : " · bo'sh");
}

/// <summary>GET /api/online-orders/courier-cash elementi — kassaga hali topshirilmagan kuryer naqdi.</summary>
public class CourierCashDto
{
    public Guid OrderId { get; set; }
    public int Number { get; set; }
    public string? CourierName { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>SignalR xabari (/hubs/orders).</summary>
public class OnlineOrderEvent
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = "";
}

public static class OnlineOrderTexts
{
    /// <summary>Telefonni yashiradi: "+998901234567" → "+998 90 *** ** 67". Format noma'lum bo'lsa oxirgi 2 raqamdan boshqasi yashiriladi.</summary>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("998"))
            return $"+998 {digits.Substring(3, 2)} *** ** {digits.Substring(10, 2)}";
        if (digits.Length <= 2) return new string('*', digits.Length);
        return new string('*', digits.Length - 2) + digits[^2..];
    }

    public static string Status(string s) => s switch
    {
        "New" => "Yangi",
        "Accepted" => "Qabul qilindi",
        "Picking" => "Yig'ilmoqda",
        "Ready" => "Tayyor",
        "AwaitingTransfer" => "O'tkazma kutilmoqda",
        "OnTheWay" => "Yo'lda",
        "ReadyForPickup" => "Olib ketishga tayyor",
        "Delivered" => "Yetkazildi",
        "Rejected" => "Rad etildi",
        "Cancelled" => "Bekor qilindi",
        _ => s
    };
}
