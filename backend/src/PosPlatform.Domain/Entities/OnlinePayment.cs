using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

/// <summary>Onlayn to'lov (Payme / Click).</summary>
public class OnlinePayment : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public PaymentProvider Provider { get; set; }

    public OnlinePaymentStatus Status { get; set; } = OnlinePaymentStatus.Pending;

    /// <summary>To'lov tizimidagi tashqi ID (Payme transactionId / Click payment_id).</summary>
    public string? ExternalId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>Mijozga yuborilgan to'lov havolasi.</summary>
    public string? PaymentUrl { get; set; }
}
