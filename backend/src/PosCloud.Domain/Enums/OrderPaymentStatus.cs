namespace PosCloud.Domain.Enums;

/// <summary>Online buyurtmaning to'lov holati (buyurtma holatidan alohida).</summary>
public enum OrderPaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Refunded = 2
}
