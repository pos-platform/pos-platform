using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders.Dtos;

public class OnlineOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public Unit Unit { get; set; }
    public bool IsWeighed { get; set; }
    public bool IsMarked { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PickedQuantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsRemoved { get; set; }
    public Guid? ReplacesItemId { get; set; }

    /// <summary>Filialdagi jami qoldiq (rezerv ichida).</summary>
    public decimal StockQuantity { get; set; }
}

public class OnlineOrderDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public OnlineOrderStatus Status { get; set; }
    public FulfillmentType FulfillmentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }

    public string? AddressLine { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public bool Contactless { get; set; }
    public string? Comment { get; set; }
    public OutOfStockPolicy OutOfStockPolicy { get; set; }

    public decimal ItemsTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    public string? TransferProofUrl { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }
    public Guid? SaleId { get; set; }
    public string? ReceiptNumber { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<OnlineOrderItemDto> Items { get; set; } = [];

    public static OnlineOrderDto FromEntity(OnlineOrder o, IReadOnlyDictionary<Guid, decimal>? stock = null) => new()
    {
        Id = o.Id,
        Number = o.Number,
        BranchId = o.BranchId,
        BranchName = o.Branch?.Name,
        Status = o.Status,
        FulfillmentType = o.FulfillmentType,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone,
        CustomerId = o.CustomerId,
        AddressLine = o.AddressLine,
        Entrance = o.Entrance,
        Floor = o.Floor,
        Apartment = o.Apartment,
        Intercom = o.Intercom,
        Landmark = o.Landmark,
        Latitude = o.Latitude,
        Longitude = o.Longitude,
        ScheduledFor = o.ScheduledFor,
        Contactless = o.Contactless,
        Comment = o.Comment,
        OutOfStockPolicy = o.OutOfStockPolicy,
        ItemsTotal = o.ItemsTotal,
        DeliveryFee = o.DeliveryFee,
        TotalAmount = o.TotalAmount,
        TransferProofUrl = o.TransferProofUrl,
        PaymentConfirmedAt = o.PaymentConfirmedAt,
        SaleId = o.SaleId,
        ReceiptNumber = o.Sale?.ReceiptNumber,
        CreatedAt = o.CreatedAt,
        AcceptedAt = o.AcceptedAt,
        ReadyAt = o.ReadyAt,
        DispatchedAt = o.DispatchedAt,
        DeliveredAt = o.DeliveredAt,
        CancelledAt = o.CancelledAt,
        CancelReason = o.CancelReason,
        Items = o.Items.OrderBy(i => i.CreatedAt).Select(i => new OnlineOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Barcode = i.Product?.Barcode,
            ImageUrl = i.Product?.ImageUrl,
            Unit = i.Product?.Unit ?? Unit.Piece,
            IsWeighed = i.Product?.IsWeighed ?? false,
            IsMarked = i.Product?.IsMarked ?? false,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            PickedQuantity = i.PickedQuantity,
            LineTotal = i.LineTotal,
            IsRemoved = i.IsRemoved,
            ReplacesItemId = i.ReplacesItemId,
            StockQuantity = stock is not null && stock.TryGetValue(i.ProductId, out var q) ? q : 0
        }).ToList()
    };
}

public class RejectOnlineOrderRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Yig'ish paytidagi o'zgarishlar.</summary>
public class UpdatePickingRequest
{
    public List<PickItemChange> Items { get; set; } = [];

    /// <summary>Yangi qator (almashtirish yoki qo'shimcha).</summary>
    public List<PickItemAdd> Add { get; set; } = [];
}

public class PickItemChange
{
    public Guid ItemId { get; set; }

    /// <summary>Haqiqatda olingan miqdor (tarozi, kam bo'lsa). Null — so'ralgan miqdor.</summary>
    public decimal? PickedQuantity { get; set; }

    public bool IsRemoved { get; set; }
}

public class PickItemAdd
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Qaysi qatorni almashtiradi (u avtomatik olib tashlanadi).</summary>
    public Guid? ReplacesItemId { get; set; }
}

public class ReadyOnlineOrderRequest
{
    /// <summary>Kassa stantsiyasi ID (fiskal agent uchun) — oddiy savdodagi kabi.</summary>
    public string? TerminalId { get; set; }

    /// <summary>Fiskal chek chiqarilsinmi (standart — ha).</summary>
    public bool Fiscalize { get; set; } = true;
}
