namespace PosCloud.Domain.Enums;

/// <summary>Savdo holati.</summary>
public enum SaleStatus
{
    /// <summary>Yaratilgan, lekin yakunlanmagan (savatcha/qoralama).</summary>
    Pending = 0,
    /// <summary>To'lov qabul qilingan va yakunlangan.</summary>
    Completed = 1,
    /// <summary>Bekor qilingan.</summary>
    Cancelled = 2,
    /// <summary>Qaytarilgan (refund qilingan) — asl savdo uchun.</summary>
    Refunded = 3
}
