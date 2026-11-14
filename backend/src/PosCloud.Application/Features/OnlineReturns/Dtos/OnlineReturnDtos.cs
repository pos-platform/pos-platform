using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineReturns.Dtos;

/// <summary>Qaytarish qatori (so'rov). Mobil ham <c>onlineOrderItemId</c>, ham <c>orderItemId</c> yuborishi mumkin.</summary>
public class CreateReturnItemRequest
{
    public Guid? OnlineOrderItemId { get; set; }
    public Guid? OrderItemId { get; set; }
    public decimal Quantity { get; set; }

    public Guid ItemId => OnlineOrderItemId ?? OrderItemId ?? Guid.Empty;
}

/// <summary>POST /api/shop/orders/{id}/returns (multipart: items JSON, reason, comment, refundMethod, photos[]).</summary>
public class CreateReturnRequest
{
    public List<CreateReturnItemRequest> Items { get; set; } = [];
    public OnlineReturnReason Reason { get; set; } = OnlineReturnReason.Other;

    [MaxLength(500)]
    public string? Comment { get; set; }

    public ReturnRefundMethod RefundMethod { get; set; } = ReturnRefundMethod.Cash;

    /// <summary>Yuklangan rasmlar (controller saqlaydi): /uploads/returns/...</summary>
    public List<string> PhotoUrls { get; set; } = [];
}

public class OnlineReturnItemDto
{
    public Guid Id { get; set; }
    public Guid OnlineOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>Qaytarish (mijoz va xodim uchun umumiy shakl; xodimga mijoz ma'lumotlari ham).</summary>
public class OnlineReturnDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid OnlineOrderId { get; set; }
    public int OrderNumber { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? StoreName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public OnlineReturnStatus Status { get; set; }
    public OnlineReturnMethod Method { get; set; }
    public OnlineReturnReason Reason { get; set; }
    public string? Comment { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public ReturnRefundMethod RefundMethod { get; set; }
    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }
    public string? CourierPhone { get; set; }
    public string? RejectReason { get; set; }
    public Guid? RefundSaleId { get; set; }
    public string? RefundReceiptNumber { get; set; }

    /// <summary>Qaytariladigan qatorlar summasi (taxminiy, buyurtma narxlari bo'yicha).</summary>
    public decimal ItemsTotal { get; set; }

    /// <summary>"Do'konga keldi"dan keyin: mijozga beriladigan summa va ushlab qolingan keshbek.</summary>
    public decimal RefundAmount { get; set; }
    public decimal LoyaltyWithheldAmount { get; set; }

    /// <summary>Kuryer olib ketishi (ReturnPickupByCourier) mumkinmi — tasdiqlashda usul tanlash uchun.</summary>
    public bool CourierPickupAllowed { get; set; }

    /// <summary>Mijoz bekor qila oladi (faqat Requested).</summary>
    public bool CanCancel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? PickupAssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public List<OnlineReturnItemDto> Items { get; set; } = [];

    public static OnlineReturnDto FromEntity(OnlineReturn r, OnlineOrder? order, string? branchName, bool includeCustomer,
        bool courierPickupAllowed, string? refundReceiptNumber = null, string? storeName = null) => new()
    {
        Id = r.Id,
        Number = r.Number,
        OnlineOrderId = r.OnlineOrderId,
        OrderNumber = order?.Number ?? 0,
        BranchId = r.BranchId,
        BranchName = branchName,
        StoreName = storeName,
        CustomerName = includeCustomer ? order?.CustomerName : null,
        CustomerPhone = includeCustomer ? order?.CustomerPhone : null,
        Status = r.Status,
        Method = r.Method,
        Reason = r.Reason,
        Comment = r.Comment,
        PhotoUrls = r.PhotoUrls.ToList(),
        RefundMethod = r.RefundMethod,
        CourierId = r.CourierId,
        CourierName = r.CourierName,
        CourierPhone = r.CourierPhone,
        RejectReason = r.RejectReason,
        RefundSaleId = r.RefundSaleId,
        RefundReceiptNumber = refundReceiptNumber,
        ItemsTotal = r.Items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2, MidpointRounding.AwayFromZero)),
        RefundAmount = r.RefundAmount,
        LoyaltyWithheldAmount = r.LoyaltyWithheldAmount,
        CourierPickupAllowed = courierPickupAllowed,
        CanCancel = r.Status == OnlineReturnStatus.Requested,
        CreatedAt = r.CreatedAt,
        ApprovedAt = r.ApprovedAt,
        RejectedAt = r.RejectedAt,
        CancelledAt = r.CancelledAt,
        PickupAssignedAt = r.PickupAssignedAt,
        PickedUpAt = r.PickedUpAt,
        ReceivedAt = r.ReceivedAt,
        RefundedAt = r.RefundedAt,
        Items = r.Items.OrderBy(i => i.CreatedAt).Select(i => new OnlineReturnItemDto
        {
            Id = i.Id,
            OnlineOrderItemId = i.OnlineOrderItemId,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ImageUrl = order?.Items.FirstOrDefault(x => x.Id == i.OnlineOrderItemId)?.Product?.ImageUrl,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = Math.Round(i.UnitPrice * i.Quantity, 2, MidpointRounding.AwayFromZero)
        }).ToList()
    };
}

public class ApproveReturnRequest
{
    public OnlineReturnMethod Method { get; set; } = OnlineReturnMethod.BringToStore;
}

public class RejectReturnRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class AssignReturnCourierRequest
{
    public Guid CourierId { get; set; }
}
