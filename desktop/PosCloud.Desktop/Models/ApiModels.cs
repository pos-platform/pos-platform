using PosCloud.Desktop.Services;

namespace PosCloud.Desktop.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";

    /// <summary>Foydalanuvchining to'liq ism-sharifi (chekda kassir nomi sifatida chiqadi).</summary>
    public string? FullName { get; set; }
    public string Role { get; set; } = "";

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

    /// <summary>
    /// Bu loginga biriktirilgan kassa stantsiyasi ID ("KASSA-1"). Bo'sh = cheklov yo'q.
    /// Server login paytida Sozlamalardagi ID bilan solishtiradi.
    /// </summary>
    public string? TerminalId { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Barcode { get; set; }
    public string MxikCode { get; set; } = "";

    /// <summary>QQS (НДС) bilanmi — fiskal chekda satr QQSi hisoblanadi.</summary>
    public bool IsVat { get; set; }

    /// <summary>Markirovka (Asl Belgisi) qilinadimi — kassada DataMatrix skanerlash majburiy.</summary>
    public bool IsMarked { get; set; }

    /// <summary>Komitent STIR/JSHSHIR — komissiya tovari bo'lsa chekda chiqadi.</summary>
    public string? CommitentTin { get; set; }
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

    /// <summary>To'g'ridan-to'g'ri mahsulotlar soni (bo'sh ota bo'limlar kassada ko'rsatilmaydi). Eski keshda null.</summary>
    public int? ProductCount { get; set; }
}

/// <summary>
/// Katalog "tamg'asi" — api/products/pos/stamp javobi. Kassa buni davriy so'rab,
/// avvalgisi bilan solishtiradi: farq bo'lsagina to'liq ro'yxat yuklanadi.
/// Soni — qo'shish/o'chirishni, vaqti — tahrirni ushlaydi.
/// Qoldiq vaqti alohida: u har savdoda o'zgaradi, shu sabab boshqacha muomala qilinadi.
/// </summary>
public class PosCatalogStamp
{
    public int ProductCount { get; set; }
    public DateTime? ProductsUpdatedAt { get; set; }
    public DateTime? StocksUpdatedAt { get; set; }
    public int CategoryCount { get; set; }
    public DateTime? CategoriesUpdatedAt { get; set; }

    /// <summary>Mahsulot kartochkalari (narx, nom, tarkib) o'zgarmaganmi.</summary>
    public bool SameProducts(PosCatalogStamp other)
        => ProductCount == other.ProductCount && ProductsUpdatedAt == other.ProductsUpdatedAt;

    /// <summary>Qoldiqlar o'zgarmaganmi.</summary>
    public bool SameStocks(PosCatalogStamp other) => StocksUpdatedAt == other.StocksUpdatedAt;

    public bool SameCategories(PosCatalogStamp other)
        => CategoryCount == other.CategoryCount && CategoriesUpdatedAt == other.CategoriesUpdatedAt;

    /// <summary>Mahsulot ro'yxati haqiqatan yuklangach — shu qismni yangilaymiz.</summary>
    public void CopyProductsFrom(PosCatalogStamp other)
    {
        ProductCount = other.ProductCount;
        ProductsUpdatedAt = other.ProductsUpdatedAt;
        StocksUpdatedAt = other.StocksUpdatedAt;
    }

    /// <summary>Kategoriyalar yuklangach — shu qismni yangilaymiz.</summary>
    public void CopyCategoriesFrom(PosCatalogStamp other)
    {
        CategoryCount = other.CategoryCount;
        CategoriesUpdatedAt = other.CategoriesUpdatedAt;
    }
}

public class ShiftDto
{
    public Guid Id { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime OpenedAt { get; set; }
    public string Status { get; set; } = "";

    // ── Z-hisobot maydonlari (faqat smena yopilganda to'ladi) ──
    public DateTime? ClosedAt { get; set; }
    public string? CashierName { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CashDifference { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalRefunds { get; set; }
    public int SaleCount { get; set; }
    public int RefundCount { get; set; }
    public decimal VatAmount { get; set; }

    /// <summary>To'lov turlari kesimi (har biri QR kodli/kodsiz ajratilgan).</summary>
    public List<ShiftPaymentLine> Payments { get; set; } = new();

    public int FiscalSaleCount { get; set; }
    public decimal FiscalSalesTotal { get; set; }
    public int NonFiscalSaleCount { get; set; }
    public decimal NonFiscalSalesTotal { get; set; }
}

/// <summary>Smena yakunidagi bitta to'lov turi (QR kodli/kodsiz ajratilgan).</summary>
public class ShiftPaymentLine
{
    /// <summary>Cash, Card, Transfer, Debt, Mixed.</summary>
    public string Method { get; set; } = "";

    public int SaleCount { get; set; }
    public decimal SaleAmount { get; set; }
    public int RefundCount { get; set; }
    public decimal RefundAmount { get; set; }

    /// <summary>Soliq QR kodi olingan (fiskalizatsiya qilingan) savdolar.</summary>
    public int FiscalCount { get; set; }
    public decimal FiscalAmount { get; set; }

    /// <summary>QR kodsiz (fiskalizatsiya qilinmagan) savdolar.</summary>
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

public class SaleItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Markirovka (Asl Belgisi) kodi — GS1 DataMatrix ning to'liq satri (markirovkali tovar uchun).</summary>
    public string? MarkingCode { get; set; }
}

public class CreateSaleRequest
{
    /// <summary>Idempotentlik kaliti — har bir savat uchun bir marta generatsiya qilinadi.
    /// Javob kelmay qayta yuborilsa server yangi savdo yaratmaydi, avvalgisini qaytaradi.</summary>
    public string? ClientSaleId { get; set; }

    public string PaymentMethod { get; set; } = "Cash";
    public decimal PaidAmount { get; set; }

    /// <summary>Aralash (Mixed) to'lovda kartadan yechilgan qism; naqd = jami − shu. Boshqa to'lovlarda 0.</summary>
    public decimal CardAmount { get; set; }
    public Guid? DebtorId { get; set; }
    /// <summary>Kassa stantsiyasi ID (masalan "KASSA-1") — fiskal agent shu bo'yicha filtrlaydi.</summary>
    public string? TerminalId { get; set; }

    /// <summary>Chek fiskalizatsiya qilinsinmi. true — yashil "To'lash va Chek chiqarish" (QR-li);
    /// false — "Chop etish" (nofiskal, QR-siz). Nasiya baribir fiskalizatsiya qilinmaydi.</summary>
    public bool Fiscalize { get; set; } = true;

    /// <summary>Bonus karta orqali tanilgan mijoz (CONTRACT-7 §4).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Ball bilan to'lanadigan qism (1 ball = 1 so'm). Server balans va
    /// maxRedeemPercent bo'yicha cheklaydi; null — ball ishlatilmaydi.</summary>
    public decimal? RedeemPoints { get; set; }

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
    public decimal CardAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? CustomerId { get; set; }

    /// <summary>Shu savdo uchun yozilgan bonus ballar.</summary>
    public decimal LoyaltyPointsEarned { get; set; }

    /// <summary>Ball bilan to'langan qism (chegirma sifatida qatorlarga taqsimlangan).</summary>
    public decimal LoyaltyPointsRedeemed { get; set; }

    // ---- Bosqich 10.5: qisman qaytarish (eski backend'da yo'q — null) ----
    /// <summary>Qaytariladigan miqdor qolganmi (server hisoblaydi).</summary>
    public bool? CanRefund { get; set; }
    /// <summary>Refund javobi: mijozdan ayirilgan keshbek ballari.</summary>
    public decimal? LoyaltyPointsReversed { get; set; }
    /// <summary>Refund javobi: ball yetmagani uchun puldan ushlab qolingan summa.</summary>
    public decimal? LoyaltyWithheldAmount { get; set; }
    /// <summary>Refund javobi: mijozga beriladigan pul.</summary>
    public decimal? RefundCashAmount { get; set; }

    public List<SaleItemDto> Items { get; set; } = new();
    public ReceiptInfo? Receipt { get; set; }
}

public class SaleItemDto
{
    /// <summary>SaleItem Id — qisman qaytarishda saleItemId sifatida yuboriladi.</summary>
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string MxikCode { get; set; } = "";
    public string Unit { get; set; } = "Piece";
    public decimal VatPercent { get; set; }
    public string? Barcode { get; set; }
    public string? CommitentTin { get; set; }

    /// <summary>Allaqachon qaytarilgan miqdor (10.5; eski backend'da null).</summary>
    public decimal? RefundedQuantity { get; set; }
    /// <summary>Hali qaytarish mumkin bo'lgan miqdor (10.5; eski backend'da null).</summary>
    public decimal? RefundableQuantity { get; set; }

    /// <summary>Qaytarish mumkin bo'lgan miqdor: server bergani yoki sotilgan − qaytarilgan.</summary>
    public decimal EffectiveRefundable =>
        RefundableQuantity ?? Math.Max(0, Quantity - (RefundedQuantity ?? 0));
}

/// <summary>POST /api/sales/{id}/refund va refund-preview tanasi (10.5).</summary>
public class RefundRequest
{
    public List<RefundItemRequest> Items { get; set; } = new();
    public string? Reason { get; set; }
}

public class RefundItemRequest
{
    public Guid SaleItemId { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>POST /api/sales/{id}/refund-preview javobi (10.5). Maydonlar tolerant.</summary>
public class RefundPreviewDto
{
    /// <summary>Qatorlar tafsiloti — shakli kelishuvda aniq emas, faqat saqlanadi.</summary>
    public System.Text.Json.JsonElement? Items { get; set; }
    public decimal RefundTotal { get; set; }
    public decimal PointsToReverse { get; set; }
    public decimal? PointsAvailable { get; set; }
    public decimal WithheldAmount { get; set; }
    public decimal CashToReturn { get; set; }
    public decimal? RedeemedPointsReturned { get; set; }
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
    /// <summary>Failed bo'lganda sabab (masalan: MXIK xato yoki modul xatosi).</summary>
    public string? ErrorMessage { get; set; }

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
    public ReceiptInfo? Receipt { get; set; }

    /// <summary>Savdo qaysi kassa stantsiyasida qilingan (bo'sh bo'lsa — kassa ID sozlanmagan).</summary>
    public string? TerminalId { get; set; }

    /// <summary>Kassa nomi ko'rinishi; TerminalId bo'sh bo'lsa "Kassasiz".</summary>
    public string TerminalLabel => string.IsNullOrWhiteSpace(TerminalId) ? NoTerminal : TerminalId!;

    /// <summary>TerminalId sozlanmagan savdolar uchun guruh nomi.</summary>
    public const string NoTerminal = "Kassasiz";

    /// <summary>Fiskal QR chiqarilgan chek (soliq QR kodi bilan sotilgan).</summary>
    public bool HasQr => !string.IsNullOrEmpty(Receipt?.QrCodeUrl);

    public string DateLabel => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string TimeLabel => CreatedAt.ToLocalTime().ToString("HH:mm");
    public string TotalLabel => (Type == "Refund" ? "− " : "") + TotalAmount.ToString("N0");
    public string TypeLabel => Type == "Refund" ? "Qaytarish"
        : Status == "Refunded" ? "Qaytarilgan"
        : Status == "PartiallyRefunded" ? "Qisman qaytarilgan"
        : "Sotuv";
    public string PaymentLabel => PaymentMethod switch
    {
        "Card" => "Karta",
        "Transfer" => "O'tkazma",
        "Mixed" => "Aralash",
        "Debt" => "Nasiya",
        _ => "Naqd"
    };
    /// <summary>Server bergan canRefund (10.5); eski backend'da null.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("canRefund")]
    public bool? ServerCanRefund { get; set; }

    /// <summary>Qaytarish mumkinmi: server bergani, bo'lmasa Sale va to'liq qaytarilmagan.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CanRefund => ServerCanRefund ?? (Type == "Sale" && Status != "Refunded");
}

/// <summary>Smena Z-hisoboti chekini chop etish uchun ma'lumot (smena + kassa sarlavhasi).</summary>
public class ShiftReportData
{
    public ShiftDto Shift { get; set; } = new();

    public string StoreName { get; set; } = "";
    public string? Stir { get; set; }
    public string? Address { get; set; }

    /// <summary>Kassa raqami (TerminalId).</summary>
    public string? Kassa { get; set; }

    public string Cashier { get; set; } = "";
}

/// <summary>Chek chop etish uchun lokal ma'lumot (sotuv paytida yig'iladi).</summary>
public class ReceiptData
{
    public string ReceiptNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;

    /// <summary>Chek sarlavhasidagi tashkilot nomi. Bo'sh bo'lsa "PosCloud" ishlatiladi.</summary>
    public string StoreName { get; set; } = "";

    /// <summary>Tashkilot STIR (ИНН) — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? Stir { get; set; }

    /// <summary>Filial manzili — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? Address { get; set; }

    /// <summary>Tashkilotning yuridik manzili (Yur.Manzil) — sarlavhada chiqadi (ixtiyoriy).</summary>
    public string? LegalAddress { get; set; }

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

    /// <summary>QQS (НДС) jami — qatorlar QQS summalarining yig'indisi.</summary>
    public decimal VatTotal => Lines.Sum(l => l.VatAmount);

    /// <summary>Aralash to'lovda karta qismi (naqd qismi = Total − CardAmount). Boshqa to'lovlarda 0.</summary>
    public decimal CardAmount { get; set; }

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

    /// <summary>Bu kassada fiskal QR alohida qurilma (masalan ФМ/fleshka) tomonidan chiqariladi.
    /// Yoqilgan bo'lsa, POS o'z QR blokini bosmaydi va fiskalizatsiyani kutmaydi —
    /// bir chekda ikkita QR (ikkilanish/double-registratsiya) chiqishining oldini oladi.</summary>
    public bool ExternalFiscalDevice { get; set; }

    /// <summary>Bonus karta egasining yashirilgan telefoni (mijoz biriktirilgan bo'lsa).</summary>
    public string? CustomerMaskedPhone { get; set; }

    /// <summary>Shu xarid uchun yozilgan bonus ballar ("Bonus: +N ball").</summary>
    public decimal LoyaltyPointsEarned { get; set; }

    /// <summary>Ball bilan to'langan qism ("Ball bilan to'landi: −M").</summary>
    public decimal LoyaltyPointsRedeemed { get; set; }

    // ---- Qaytarish cheki (10.5) ----
    /// <summary>Qaytarish (refund) cheki — sarlavha "Qaytarish", "Berilgan/Qaytim" o'rniga qaytarish bloki.</summary>
    public bool IsRefund { get; set; }
    /// <summary>Ball yetmagani uchun puldan ushlab qolingan keshbek ("Keshbek ushlab qolindi: X").</summary>
    public decimal LoyaltyWithheldAmount { get; set; }
    /// <summary>Mijozdan ayirilgan keshbek ballari.</summary>
    public decimal LoyaltyPointsReversed { get; set; }
    /// <summary>Mijozga beriladigan pul (null — Total).</summary>
    public decimal? RefundCashAmount { get; set; }
    /// <summary>Qaytarish sababi (ixtiyoriy).</summary>
    public string? RefundReason { get; set; }
}

public class ReceiptLine
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Qator chegirmasi (masalan ball bilan to'lov taqsimoti) — bo'lsa chekda chiqadi.</summary>
    public decimal Discount { get; set; }

    /// <summary>MXIK (ИКПУ) kodi — fiskal chekda mahsulot ostida chiqadi.</summary>
    public string MxikCode { get; set; } = "";

    /// <summary>Shtrix-kod (ШК) — fiskal chekda mahsulot ostida chiqadi (ixtiyoriy).</summary>
    public string? Barcode { get; set; }

    /// <summary>Markirovka (Asl Belgisi) kodi — chekda "МК:" qatorida chiqadi (ixtiyoriy).</summary>
    public string? MarkingCode { get; set; }

    /// <summary>QQS (НДС) bilanmi — narxga kiritilgan 12%.</summary>
    public bool IsVat { get; set; }

    /// <summary>O'lchov birligi: "Piece" yoki "Kilogram" — chekda "d." / "kg" bo'lib chiqadi (VMQ-943, 9-band).</summary>
    public string Unit { get; set; } = "Piece";

    public string UnitLabel => Unit == "Kilogram" ? "kg" : "d.";

    /// <summary>Komitent STIR/JSHSHIR — komissiya tovarida chekda chiqadi (14-band, ixtiyoriy).</summary>
    public string? CommitentTin { get; set; }

    /// <summary>Qator QQS stavkasi (%) — chekda har tovar ostida chiqadi (12-band).</summary>
    public decimal VatPercent => IsVat ? 12m : 0m;

    /// <summary>Qator QQS summasi — narxga kiritilgan (13-band).</summary>
    public decimal VatAmount => IsVat ? Math.Round(LineTotal * 12m / 112m, 2) : 0m;
}

/// <summary>POST /api/loyalty/resolve javobi — bonus karta QR'idan tanilgan mijoz (CONTRACT-7 §3).</summary>
public class LoyaltyCustomerDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string? MaskedPhone { get; set; }
    public decimal Points { get; set; }
    public decimal MaxRedeemPercent { get; set; }
    /// <summary>Loyalty darajasi nomi (CONTRACT-9 §9.4); daraja yo'q yoki eski backend — null.</summary>
    public string? TierName { get; set; }
}
