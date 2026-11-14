using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Sales.Dtos;

/// <summary>
/// POST /api/sales/{id}/refund (va refund-preview) body'si — ixtiyoriy.
/// Body yo'q yoki items bo'sh — to'liq qaytarish (qolgan barcha miqdor).
/// </summary>
public class RefundRequest
{
    [MaxLength(200)]
    public List<RefundItemRequest>? Items { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class RefundItemRequest
{
    public Guid SaleItemId { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>Ichki (servislararo) qaytarish sozlamalari — API orqali kelmaydi.</summary>
public class RefundOptions
{
    /// <summary>false — ombor qoldig'i bu yerda qaytmaydi (eshik oldida rad etish: tovar do'konga kelganda qaytadi).</summary>
    public bool ReturnStock { get; set; } = true;

    /// <summary>true — joriy foydalanuvchida ochiq smena bo'lmasa ham (kuryer) qaytarish yoziladi (smenasiz).</summary>
    public bool AllowWithoutShift { get; set; }

    /// <summary>Pul o'rniga bonus ball (online qaytarish, RefundMethod = Bonus). Refund smenaga yozilmaydi.</summary>
    public bool CashAsBonus { get; set; }

    /// <summary>Refund to'lov usuli (masalan, o'tkazma bilan qaytarish). Null — asl savdo usuli.</summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Bonus harakati izohiga qo'shiladi (masalan, "Online #1045").</summary>
    public string? Note { get; set; }
}

/// <summary>POST /api/sales/{id}/refund-preview javobi.</summary>
public class RefundPreviewDto
{
    public List<RefundPreviewItemDto> Items { get; set; } = [];

    /// <summary>Qaytarilgan qatorlar summasi (ushlab qolishdan oldin).</summary>
    public decimal RefundTotal { get; set; }

    /// <summary>Mijozdan ayiriladigan keshbek (ball).</summary>
    public decimal PointsToReverse { get; set; }

    /// <summary>Mijozning hozirgi balli (ball bilan to'langan qism qaytarilgandan keyin).</summary>
    public decimal PointsAvailable { get; set; }

    /// <summary>Ball yetmagani uchun puldan ushlab qolinadigan summa.</summary>
    public decimal WithheldAmount { get; set; }

    /// <summary>Mijozga beriladigan pul.</summary>
    public decimal CashToReturn { get; set; }

    /// <summary>Ball bilan to'langan qismdan ballga qaytariladigani.</summary>
    public decimal RedeemedPointsReturned { get; set; }
}

public class RefundPreviewItemDto
{
    public Guid SaleItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
