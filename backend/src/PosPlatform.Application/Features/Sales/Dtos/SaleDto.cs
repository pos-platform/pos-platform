using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Sales.Dtos;

public class SaleDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public Guid CashierId { get; set; }
    public Guid? CashShiftId { get; set; }
    public SaleType Type { get; set; }
    public Guid? OriginalSaleId { get; set; }
    public SaleStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal LoyaltyPointsEarned { get; set; }
    public Guid? TableId { get; set; }
    public Guid? WaiterId { get; set; }
    public int? GuestCount { get; set; }
    public decimal ServiceChargePercent { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
    public ReceiptDto? Receipt { get; set; }

    public static SaleDto FromEntity(Sale s) => new()
    {
        Id = s.Id,
        ReceiptNumber = s.ReceiptNumber,
        BranchId = s.BranchId,
        CashierId = s.CashierId,
        CashShiftId = s.CashShiftId,
        Type = s.Type,
        OriginalSaleId = s.OriginalSaleId,
        Status = s.Status,
        PaymentMethod = s.PaymentMethod,
        TotalAmount = s.TotalAmount,
        DiscountAmount = s.DiscountAmount,
        VatAmount = s.VatAmount,
        PaidAmount = s.PaidAmount,
        ChangeAmount = s.ChangeAmount,
        CustomerId = s.CustomerId,
        LoyaltyPointsEarned = s.LoyaltyPointsEarned,
        TableId = s.TableId,
        WaiterId = s.WaiterId,
        GuestCount = s.GuestCount,
        ServiceChargePercent = s.ServiceChargePercent,
        ServiceChargeAmount = s.ServiceChargeAmount,
        OpenedAt = s.OpenedAt,
        CreatedAt = s.CreatedAt,
        Items = s.Items.Select(SaleItemDto.FromEntity).ToList(),
        Receipt = s.Receipt is null ? null : ReceiptDto.FromEntity(s.Receipt)
    };
}

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MxikCode { get; set; } = string.Empty;
    public Unit Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
    public bool SentToKitchen { get; set; }

    public static SaleItemDto FromEntity(SaleItem i) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ProductName = i.ProductName,
        MxikCode = i.MxikCode,
        Unit = i.Unit,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        CostPrice = i.CostPrice,
        DiscountAmount = i.DiscountAmount,
        VatPercent = i.VatPercent,
        VatAmount = i.VatAmount,
        LineTotal = i.LineTotal,
        Note = i.Note,
        SentToKitchen = i.SentToKitchen
    };
}

public class ReceiptDto
{
    public Guid Id { get; set; }
    public FiscalStatus FiscalStatus { get; set; }
    public string? FiscalSign { get; set; }
    public string? FiscalModuleId { get; set; }
    public string? FiscalRegistrationNumber { get; set; }
    public string? FiscalReceiptNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTime? FiscalizedAt { get; set; }

    public static ReceiptDto FromEntity(Receipt r) => new()
    {
        Id = r.Id,
        FiscalStatus = r.FiscalStatus,
        FiscalSign = r.FiscalSign,
        FiscalModuleId = r.FiscalModuleId,
        FiscalRegistrationNumber = r.FiscalRegistrationNumber,
        FiscalReceiptNumber = r.FiscalReceiptNumber,
        QrCodeUrl = r.QrCodeUrl,
        FiscalizedAt = r.FiscalizedAt
    };
}
