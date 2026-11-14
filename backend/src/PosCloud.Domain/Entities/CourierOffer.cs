using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>Tayyor buyurtma bo'yicha bitta kuryerga yuborilgan taklif. Birinchi qabul qilgan buyurtmani oladi.</summary>
public class CourierOffer : BaseEntity
{
    public Guid OnlineOrderId { get; set; }
    public OnlineOrder? OnlineOrder { get; set; }

    public Guid CourierUserId { get; set; }
    public User? CourierUser { get; set; }

    public CourierOfferItemStatus Status { get; set; } = CourierOfferItemStatus.Pending;

    /// <summary>Taklif amal qilish muddati (CreatedAt + CourierOfferTimeoutSec).</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }
    public string? DeclineReason { get; set; }
}

/// <summary>Xodim (kuryer) qurilmasining FCM tokeni — taklif push'lari uchun.</summary>
public class StaffDeviceToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>FCM registration token (unikal).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>"android" yoki "ios".</summary>
    public string Platform { get; set; } = string.Empty;
}
