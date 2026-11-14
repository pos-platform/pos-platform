using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Courier.Dtos;

/// <summary>GET /api/courier/me</summary>
public class CourierMeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string StoreName { get; set; } = string.Empty;
}

/// <summary>Kuryer buyurtmasi (ro'yxat elementi).</summary>
public class CourierOrderDto
{
    /// <summary>"delivery" (yetkazish) — qaytarish vazifalari alohida (CourierReturnTaskDto, "return_pickup").</summary>
    public string Kind { get; set; } = "delivery";
    public Guid Id { get; set; }
    public int Number { get; set; }
    public OnlineOrderStatus Status { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Intercom { get; set; }
    public string? Landmark { get; set; }
    public string? Comment { get; set; }
    public bool Contactless { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; }
    public int ItemCount { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? BranchAddress { get; set; }
    public double? BranchLatitude { get; set; }
    public double? BranchLongitude { get; set; }
}

public class CourierOrderItemDto
{
    /// <summary>OnlineOrderItem ID — "delivered"da rejectedItems[].itemId uchun.</summary>
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal RejectedQuantity { get; set; }
}

/// <summary>GET /api/courier/orders/{id}</summary>
public class CourierOrderDetailDto : CourierOrderDto
{
    public List<CourierOrderItemDto> Items { get; set; } = [];
}

/// <summary>Kuryer vazifasi: qaytarishni mijozdan olib ketish (GET /api/courier/orders ro'yxatida, kind = "return_pickup").</summary>
public class CourierReturnTaskDto
{
    public string Kind { get; set; } = "return_pickup";

    /// <summary>Qaytarish ID (picked-up uchun).</summary>
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public OnlineReturnStatus Status { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Entrance { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Landmark { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Comment { get; set; }
    public DateTime? PickupAssignedAt { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? BranchAddress { get; set; }
    public List<CourierOrderItemDto> Items { get; set; } = [];
}

/// <summary>Eshik oldida mijoz olmagan qator.</summary>
public class CourierRejectedItemRequest
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }
}

public class CourierDeliveredRequest
{
    /// <summary>Naqd buyurtmada mijozdan olingan summa (bo'sh — buyurtma summasi, rad etilganlar ayirilgandan keyin).</summary>
    [Range(0, 1_000_000_000)]
    public decimal? CashCollected { get; set; }

    /// <summary>Eshik oldida rad etilgan tovarlar (ixtiyoriy) — buyurtma savdosiga avtomatik qisman qaytarish.</summary>
    [MaxLength(100)]
    public List<CourierRejectedItemRequest>? RejectedItems { get; set; }
}

public class CourierFailedRequest
{
    [Required]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}
