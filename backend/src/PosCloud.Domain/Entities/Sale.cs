using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Bitta savdo (tranzaksiya) — bir nechta qatordan iborat.</summary>
public class Sale : BaseEntity
{
    /// <summary>Ichki chek raqami (inson o'qiy oladigan, masalan R-20260601-0007).</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Kassa (mijoz) tomonidan berilgan takrorlanmas kalit — idempotentlik uchun.
    /// Kassa so'rov yubordi-yu javobni ololmasa (tarmoq uzildi, vaqt tugadi), u xuddi shu
    /// kalit bilan qayta yuboradi; server yangi savdo yaratmay, o'sha savdoning o'zini
    /// qaytaradi. Shu tufayli bitta savdo ikki marta o'tib ketmaydi (qoldiq ham ikki
    /// marta kamaymaydi). Eski mijozlar bu maydonni yubormaydi — null bo'lishi mumkin.
    /// </summary>
    public string? ClientSaleId { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Savdoni amalga oshirgan kassir (User).</summary>
    public Guid CashierId { get; set; }
    public User? Cashier { get; set; }

    /// <summary>Savdo qaysi smenaga (kassa sessiyasiga) tegishli.</summary>
    public Guid? CashShiftId { get; set; }
    public CashShift? CashShift { get; set; }

    /// <summary>
    /// Savdo qaysi kassa stantsiyasida (PC/terminal) amalga oshirilgani — masalan "KASSA-1".
    /// Fiskal agent shu qiymat bo'yicha faqat o'z terminaliga tegishli cheklarni oladi
    /// (har terminalda o'z fiskal moduli bo'lganda). Bo'sh bo'lsa — bitta agent hammasini oladi.
    /// </summary>
    public string? TerminalId { get; set; }

    public SaleType Type { get; set; } = SaleType.Sale;

    /// <summary>Refund bo'lsa — qaytarilayotgan asl savdo.</summary>
    public Guid? OriginalSaleId { get; set; }
    public Sale? OriginalSale { get; set; }

    /// <summary>Nasiya (Debt) sotuvda — qaysi qarzdorga yozilgani.</summary>
    public Guid? DebtorId { get; set; }
    public Debtor? Debtor { get; set; }

    /// <summary>Mijoz (sodiqlik dasturi uchun ixtiyoriy).</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Ushbu savdodan topilgan ball.</summary>
    public decimal LoyaltyPointsEarned { get; set; }

    // --- Restoran (ochiq buyurtma) metadatasi ---

    /// <summary>Restoran stoli — ochiq buyurtma qaysi stolga tegishli (ixtiyoriy).</summary>
    public Guid? TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    /// <summary>Buyurtmani olgan ofitsiant (User, ixtiyoriy).</summary>
    public Guid? WaiterId { get; set; }
    public User? Waiter { get; set; }

    /// <summary>Mehmonlar soni (ixtiyoriy).</summary>
    public int? GuestCount { get; set; }

    /// <summary>Servis haqi foizi (%).</summary>
    public decimal ServiceChargePercent { get; set; }

    /// <summary>Servis haqi summasi.</summary>
    public decimal ServiceChargeAmount { get; set; }

    /// <summary>Ochiq buyurtma ochilgan vaqt (Pending bo'lganda).</summary>
    public DateTime? OpenedAt { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>O'tkazma (Transfer) to'lovda qaysi usul ishlatilgani: "Payme" yoki "Karta". Boshqa to'lovlarda null.</summary>
    public string? TransferSource { get; set; }

    /// <summary>Qatorlar yig'indisi minus chegirma (mijoz to'lashi kerak bo'lgan summa).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Umumiy chegirma summasi.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Barcha qatorlardagi QQS (VAT) yig'indisi.</summary>
    public decimal VatAmount { get; set; }

    /// <summary>Mijoz bergan summa.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Qaytim.</summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>
    /// Aralash (Mixed) to'lovda kartadan yechilgan qism; naqd qismi = TotalAmount − CardAmount.
    /// Boshqa to'lovlarda 0. Fiskal chekda ReceivedCash/ReceivedCard shu bo'yicha ajratiladi.
    /// </summary>
    public decimal CardAmount { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    /// <summary>Ushbu savdoning fiskal cheki (1:1).</summary>
    public Receipt? Receipt { get; set; }
}
