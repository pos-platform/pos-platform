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

    // ---- Bosqich 9.3: almashtirish siyosati va baho (eski backend'da yo'q — null) ----
    /// <summary>Replace | Call | Remove (std Call). Eski backend'da null — <see cref="OutOfStockPolicy"/> ishlatiladi.</summary>
    public string? SubstitutionPolicy { get; set; }
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }
    public List<string>? RatingTags { get; set; }
    public DateTime? RatedAt { get; set; }

    public List<OnlineOrderItemDto> Items { get; set; } = new();

    // ---- ko'rinish uchun ----

    /// <summary>Normallashtirilgan siyosat: Replace | Call | Remove.</summary>
    public string SubstitutionKind
    {
        get
        {
            var p = string.IsNullOrWhiteSpace(SubstitutionPolicy) ? OutOfStockPolicy : SubstitutionPolicy;
            return p switch
            {
                "Replace" or "ReplaceSimilar" => "Replace",
                "Remove" => "Remove",
                _ => "Call"
            };
        }
    }

    /// <summary>"Bo'lmasa: almashtiring / qo'ng'iroq qiling / olib tashlang".</summary>
    public string SubstitutionText => SubstitutionKind switch
    {
        "Replace" => "Bo'lmasa: almashtiring",
        "Remove" => "Bo'lmasa: olib tashlang",
        _ => "Bo'lmasa: qo'ng'iroq qiling"
    };

    public System.Windows.Media.Brush SubstitutionBackground => new System.Windows.Media.SolidColorBrush(SubstitutionKind switch
    {
        "Replace" => System.Windows.Media.Color.FromRgb(0xE1, 0xF2, 0xE8),
        "Remove" => System.Windows.Media.Color.FromRgb(0xFB, 0xE4, 0xE4),
        _ => System.Windows.Media.Color.FromRgb(0xE3, 0xEB, 0xFB)
    });

    public System.Windows.Media.Brush SubstitutionForeground => new System.Windows.Media.SolidColorBrush(SubstitutionKind switch
    {
        "Replace" => System.Windows.Media.Color.FromRgb(0x0B, 0x6B, 0x45),
        "Remove" => System.Windows.Media.Color.FromRgb(0xB4, 0x23, 0x18),
        _ => System.Windows.Media.Color.FromRgb(0x2D, 0x5B, 0xD0)
    });

    public bool HasRating => Rating is >= 1 and <= 5;

    /// <summary>"⭐⭐⭐⭐☆ 4/5 · Tez, Yangi · «izoh»" yoki bo'sh.</summary>
    public string RatingText
    {
        get
        {
            if (!HasRating) return "";
            var r = Rating!.Value;
            var text = "Baho: " + new string('★', r) + new string('☆', 5 - r) + $" {r}/5";
            var tags = (RatingTags ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(OnlineOrderTexts.RatingTag).ToList();
            if (tags.Count > 0) text += " · " + string.Join(", ", tags);
            if (!string.IsNullOrWhiteSpace(RatingComment)) text += $"\n«{RatingComment!.Trim()}»";
            if (RatedAt is { } at) text += $"\n{at.ToLocalTime():dd.MM.yyyy HH:mm}";
            return text;
        }
    }
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

    /// <summary>Mahsulot bo'lmasa nima qilish — 9.3 dan <see cref="SubstitutionPolicy"/>, eski backend'da OutOfStockPolicy.</summary>
    public string PolicyText => SubstitutionText;
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

    /// <summary>Baho tegi: fast|fresh|polite|accurate|late|damaged|missing|wrong_item → o'zbekcha.</summary>
    public static string RatingTag(string tag) => tag.Trim().ToLowerInvariant() switch
    {
        "fast" => "Tez",
        "fresh" => "Yangi mahsulot",
        "polite" => "Xushmuomala",
        "accurate" => "Aniq yig'ilgan",
        "late" => "Kechikdi",
        "damaged" => "Shikastlangan",
        "missing" => "Yetishmadi",
        "wrong_item" => "Noto'g'ri mahsulot",
        _ => tag
    };

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
