using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Fiscal.Dtos;

/// <summary>
/// Lokal fiskal agent uchun fiskalizatsiya kutayotgan chek ma'lumoti.
/// </summary>
public class PendingReceiptDto
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? BranchId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public List<PendingReceiptItemDto> Items { get; set; } = new();

    public static PendingReceiptDto FromEntity(Sale s) => new()
    {
        SaleId = s.Id,
        ReceiptNumber = s.ReceiptNumber,
        CreatedAt = s.CreatedAt,
        BranchId = s.BranchId,
        PaymentMethod = s.PaymentMethod,
        TotalAmount = s.TotalAmount,
        VatAmount = s.VatAmount,
        Items = s.Items.Select(i => new PendingReceiptItemDto
        {
            ProductName = i.ProductName,
            MxikCode = i.MxikCode,
            Barcode = i.Barcode,
            Unit = i.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            VatPercent = i.VatPercent,
            VatAmount = i.VatAmount,
            LineTotal = i.LineTotal
        }).ToList()
    };
}

public class PendingReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public Unit Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
}
