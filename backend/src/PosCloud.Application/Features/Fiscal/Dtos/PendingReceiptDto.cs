using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Fiscal.Dtos;

/// <summary>
/// Lokal fiskal agent uchun fiskalizatsiya kutayotgan chek ma'lumoti.
/// </summary>
public class PendingReceiptDto
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>Tadbirkorlik subyekti nomi — chek sarlavhasi (VMQ-943, 1-band).</summary>
    public string? OrganizationName { get; set; }

    /// <summary>Tashkilot STIR — chek sarlavhasi (3-band).</summary>
    public string? OrganizationStir { get; set; }

    /// <summary>Savdo amalga oshirilgan manzil (filial manzili) — chek sarlavhasi (2-band).</summary>
    public string? Address { get; set; }

    /// <summary>Chek turi: Sale — sotuv, Refund — qaytarish (agent modulga qaytarish operatsiyasini yuboradi).</summary>
    public SaleType Type { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }

    /// <summary>Aralash to'lovda karta qismi (naqd = TotalAmount − CardAmount); boshqalarda 0.</summary>
    public decimal CardAmount { get; set; }

    /// <summary>
    /// Qaytarish chekida — asl fiskal chek rekvizitlari (soliq qaytarishni shu chekka bog'laydi).
    /// Sotuv chekida null.
    /// </summary>
    public RefundInfoDto? RefundInfo { get; set; }

    public List<PendingReceiptItemDto> Items { get; set; } = new();

    public static PendingReceiptDto FromEntity(Sale s) => new()
    {
        SaleId = s.Id,
        ReceiptNumber = s.ReceiptNumber,
        CreatedAt = s.CreatedAt,
        BranchId = s.BranchId,
        OrganizationName = s.Branch?.Organization?.Name ?? s.Branch?.Name,
        OrganizationStir = s.Branch?.Organization?.Stir,
        Address = s.Branch?.Address,
        Type = s.Type,
        PaymentMethod = s.PaymentMethod,
        TotalAmount = s.TotalAmount,
        VatAmount = s.VatAmount,
        CardAmount = s.CardAmount,
        RefundInfo = s.Type == SaleType.Refund ? RefundInfoDto.FromReceipt(s.OriginalSale?.Receipt) : null,
        Items = s.Items.Select(i => new PendingReceiptItemDto
        {
            ProductName = i.ProductName,
            MxikCode = i.MxikCode,
            Barcode = i.Barcode,
            MarkingCode = i.MarkingCode,
            PackageCode = i.PackageCode,
            CommitentTin = i.CommitentTin,
            Unit = i.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            VatPercent = i.VatPercent,
            VatAmount = i.VatAmount,
            LineTotal = i.LineTotal
        }).ToList()
    };
}

/// <summary>Qaytarilayotgan asl fiskal chekning rekvizitlari (OFD RefundInfo).</summary>
public class RefundInfoDto
{
    /// <summary>Asl chekni ro'yxatga olgan fiskal modul (TerminalID).</summary>
    public string TerminalId { get; set; } = string.Empty;

    /// <summary>Asl chekning moduldagi tartib raqami (ReceiptSeq).</summary>
    public string ReceiptSeq { get; set; } = string.Empty;

    /// <summary>Asl chek fiskalizatsiya vaqti (UTC).</summary>
    public DateTime DateTime { get; set; }

    /// <summary>Asl chekning fiskal belgisi.</summary>
    public string FiscalSign { get; set; } = string.Empty;

    /// <summary>Asl chek to'liq fiskallangan bo'lsa rekvizitlar, aks holda null.</summary>
    public static RefundInfoDto? FromReceipt(Receipt? r)
    {
        if (r is null
            || string.IsNullOrWhiteSpace(r.FiscalModuleId)
            || string.IsNullOrWhiteSpace(r.FiscalReceiptNumber)
            || string.IsNullOrWhiteSpace(r.FiscalSign)
            || r.FiscalizedAt is null)
            return null;

        return new RefundInfoDto
        {
            TerminalId = r.FiscalModuleId,
            ReceiptSeq = r.FiscalReceiptNumber,
            DateTime = r.FiscalizedAt.Value,
            FiscalSign = r.FiscalSign
        };
    }
}

public class PendingReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    /// <summary>Markirovka (Asl Belgisi) kodi — fiskal chekning Labels maydoniga ketadi (ixtiyoriy).</summary>
    public string? MarkingCode { get; set; }

    /// <summary>Qadoq kodi (Код упаковки, tasnif.soliq.uz) — fiskal chek qatorining PackageCode maydoni.</summary>
    public string? PackageCode { get; set; }

    /// <summary>Komitent STIR (9) / JSHSHIR (14) — fiskal chek qatorining CommissionInfo maydoni (ixtiyoriy).</summary>
    public string? CommitentTin { get; set; }
    public Unit Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
}
