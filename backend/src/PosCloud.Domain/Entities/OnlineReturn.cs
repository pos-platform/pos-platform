using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Yetkazilgan online buyurtmani qaytarish so'rovi (mijoz → do'kon tasdiqlaydi → kuryer olib ketadi yoki mijoz olib keladi
/// → "Do'konga keldi"da qisman refund → "Pul berildi"). OrganizationId qo'lda to'ldiriladi (mijoz so'rovida tenant yo'q).
/// </summary>
public class OnlineReturn : BaseEntity
{
    public Guid OnlineOrderId { get; set; }
    public OnlineOrder? OnlineOrder { get; set; }

    public Guid BranchId { get; set; }

    public Guid ShopUserId { get; set; }

    /// <summary>Tashkilot ichida ketma-ket raqam.</summary>
    public int Number { get; set; }

    public OnlineReturnStatus Status { get; set; } = OnlineReturnStatus.Requested;
    public OnlineReturnMethod Method { get; set; } = OnlineReturnMethod.BringToStore;
    public OnlineReturnReason Reason { get; set; } = OnlineReturnReason.Other;
    public string? Comment { get; set; }

    /// <summary>Mijoz yuklagan rasmlar (≤3).</summary>
    public List<string> PhotoUrls { get; set; } = new();

    public ReturnRefundMethod RefundMethod { get; set; } = ReturnRefundMethod.Cash;

    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }
    public string? CourierPhone { get; set; }
    public string? RejectReason { get; set; }

    /// <summary>"Do'konga keldi"da yaratilgan refund savdosi.</summary>
    public Guid? RefundSaleId { get; set; }

    /// <summary>Mijozga qaytariladigan summa (keshbek ushlab qolingandan keyin).</summary>
    public decimal RefundAmount { get; set; }
    public decimal LoyaltyWithheldAmount { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? PickupAssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public ICollection<OnlineReturnItem> Items { get; set; } = new List<OnlineReturnItem>();
}

/// <summary>Qaytariladigan qator (buyurtma qatoridan nusxa).</summary>
public class OnlineReturnItem : BaseEntity
{
    public Guid OnlineReturnId { get; set; }
    public OnlineReturn? OnlineReturn { get; set; }

    public Guid OnlineOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
