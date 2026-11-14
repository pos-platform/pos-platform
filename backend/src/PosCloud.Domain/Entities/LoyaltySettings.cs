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

/// <summary>
/// Loyalty darajasi (Silver, Gold...). Mijozning oxirgi 365 kundagi xaridi MinSpend'dan oshsa — shu foiz
/// (bazaviy/kategoriya foizidan katta bo'lsa) qo'llanadi.
/// </summary>
public class LoyaltyTier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MinSpend { get; set; }
    public decimal CashbackPercent { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Kategoriya bo'yicha alohida cashback foizi (tashkilot bo'yicha).</summary>
public class LoyaltyCategoryRate : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Cashback foizi (0..100).</summary>
    public decimal Percent { get; set; }
}

/// <summary>Bonus sozlamalari o'zgarishlari tarixi (har o'zgargan maydon — alohida yozuv).</summary>
public class LoyaltySettingsChange : BaseEntity
{
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    /// <summary>Maydon: cashbackPercent, minSaleAmount, category:NOM, tier:NOM ...</summary>
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
