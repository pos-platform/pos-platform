using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Sales.Dtos;

/// <summary>Stolda yangi ochiq buyurtma ochish.</summary>
public class OpenOrderRequest
{
    [Required]
    public Guid TableId { get; set; }
    public int? GuestCount { get; set; }
    public Guid? BranchId { get; set; }
}

/// <summary>Ochiq buyurtma qatorlarini saqlash (to'liq ro'yxat — set semantikasi).</summary>
public class SaveOrderItemsRequest
{
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>Qator izohi / modifikator (masalan "shakarsiz").</summary>
    public string? Note { get; set; }

    public decimal? UnitPriceOverride { get; set; }
}

/// <summary>Ochiq buyurtmani to'lash (yopish) — fiskal chek yaratiladi.</summary>
public class PayOrderRequest
{
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public Guid? DebtorId { get; set; }
    public Guid? CustomerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    /// <summary>Servis haqi foizi (%).</summary>
    [Range(0, 100)]
    public decimal ServiceChargePercent { get; set; }

    /// <summary>
    /// Buyurtmani yopgan kassa stantsiyasi ID. Fiskal navbat shu bo'yicha filtrlanadi va
    /// kunlik savdolar kassalar kesimida to'g'ri guruhlanadi.
    /// </summary>
    [MaxLength(64)]
    public string? TerminalId { get; set; }
}
