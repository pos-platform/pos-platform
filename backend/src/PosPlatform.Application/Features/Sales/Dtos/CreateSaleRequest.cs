using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Sales.Dtos;

public class CreateSaleRequest
{
    /// <summary>Filial. Bo'sh bo'lsa, joriy kassirning filiali olinadi.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Idempotentlik kaliti — kassa har bir savat uchun bir marta generatsiya qiladi.
    /// Javob yetib bormaganda kassa xuddi shu kalit bilan qayta yuboradi va server
    /// yangi savdo yaratmay, avvalgisini qaytaradi (takroriy savdo bo'lmaydi).
    /// Ixtiyoriy: eski kassa versiyalari bu maydonsiz ishlayveradi.
    /// </summary>
    [MaxLength(64)]
    public string? ClientSaleId { get; set; }

    /// <summary>
    /// Kassa stantsiyasi (PC/terminal) identifikatori, masalan "KASSA-1".
    /// Desktop shu qiymatni yuboradi; fiskal agent shu bo'yicha o'z terminalining cheklarini oladi.
    /// </summary>
    public string? TerminalId { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>
    /// Chek fiskalizatsiya qilinsinmi (soliq tizimiga yuborilib QR kod olsinmi).
    /// <c>true</c> — yashil "To'lash va Chek chiqarish" (fiskal, QR-li chek).
    /// <c>false</c> — "Chop etish" (nofiskal, QR-siz chek): soliqqa umuman yuborilmaydi.
    /// Nasiya (Debt) esa bu qiymatdan qat'i nazar hech qachon fiskalizatsiya qilinmaydi.
    /// </summary>
    public bool Fiscalize { get; set; } = true;

    /// <summary>O'tkazma (Transfer) to'lovda qaysi usul: "Payme" yoki "Karta". Boshqa to'lovlarda e'tiborga olinmaydi.</summary>
    [MaxLength(30)]
    public string? TransferSource { get; set; }

    /// <summary>Nasiya (Debt) sotuvda majburiy — qaysi qarzdorga.</summary>
    public Guid? DebtorId { get; set; }

    /// <summary>Mijoz (sodiqlik dasturi, ixtiyoriy).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Mijoz bergan summa (naqd uchun qaytim hisoblanadi).</summary>
    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSaleItemRequest> Items { get; set; } = new();
}

public class CreateSaleItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>Narxni qo'lda o'zgartirish (bo'sh bo'lsa mahsulot narxi olinadi).</summary>
    public decimal? UnitPriceOverride { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    /// <summary>QQS stavkasi (%). Bo'sh bo'lsa standart stavka qo'llanadi.</summary>
    public decimal? VatPercent { get; set; }

    /// <summary>
    /// Markirovka (Asl Belgisi) kodi — GS1 DataMatrix ning to'liq satri, kassada skanerlanadi.
    /// Markirovkali (IsMarked) tovar uchun majburiy; fiskal chekning Labels maydoniga uzatiladi.
    /// </summary>
    [MaxLength(256)]
    public string? MarkingCode { get; set; }
}
