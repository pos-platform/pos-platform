using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Tashkilotning bonus (cashback) dasturi sozlamalari. Har tashkilotga bitta; yozuv bo'lmasa standart qiymatlar ishlaydi.
/// </summary>
public class LoyaltySettings : BaseEntity
{
    public bool Enabled { get; set; } = true;

    /// <summary>Standart cashback foizi (0..100).</summary>
    public decimal CashbackPercent { get; set; } = 1m;

    /// <summary>Shu summadan kam savdoga ball berilmaydi.</summary>
    public decimal MinSaleAmount { get; set; }

    /// <summary>Savdo jamisining necha foizini ball bilan to'lash mumkin (0..100).</summary>
    public decimal MaxRedeemPercent { get; set; } = 50m;

    /// <summary>Faolsizlik kunlari — shundan keyin ball yonadi. null — muddatsiz.</summary>
    public int? PointsExpireDays { get; set; }

    /// <summary>Ilova orqali birinchi bog'langanda bir marta beriladigan bonus.</summary>
    public decimal SignupBonus { get; set; }

    /// <summary>false — chegirmali (aksiya/promo/bonus) qatorlarga ball berilmaydi.</summary>
    public bool EarnOnDiscountedItems { get; set; } = true;
}

/// <summary>Kategoriya bo'yicha alohida cashback foizi (tashkilot bo'yicha).</summary>
public class LoyaltyCategoryRate : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Cashback foizi (0..100).</summary>
    public decimal Percent { get; set; }
}
