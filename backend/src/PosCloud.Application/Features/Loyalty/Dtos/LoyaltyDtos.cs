using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty.Dtos;

/// <summary>Bonus dasturi sozlamalari (javob).</summary>
public class LoyaltySettingsDto
{
    public bool Enabled { get; set; }
    public decimal CashbackPercent { get; set; }
    public decimal MinSaleAmount { get; set; }
    public decimal MaxRedeemPercent { get; set; }

    /// <summary>Faolsizlik kunlari; null — muddatsiz.</summary>
    public int? PointsExpireDays { get; set; }

    public decimal SignupBonus { get; set; }
    public bool EarnOnDiscountedItems { get; set; }
    public List<LoyaltyCategoryRateDto> CategoryRates { get; set; } = new();

    /// <summary>Darajalar (minSpend o'sish tartibida).</summary>
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
}

/// <summary>Loyalty darajasi: oxirgi 365 kunda minSpend'dan ko'p xarid qilgan mijozga cashbackPercent.</summary>
public class LoyaltyTierDto
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal MinSpend { get; set; }

    [Range(0, 100)]
    public decimal CashbackPercent { get; set; }
}

public class LoyaltyCategoryRateDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Percent { get; set; }
}

/// <summary>Sozlamalarni saqlash. <c>categoryRates</c> to'liq ro'yxat sifatida almashtiriladi.</summary>
public class UpdateLoyaltySettingsRequest
{
    public bool Enabled { get; set; } = true;

    [Range(0, 100)]
    public decimal CashbackPercent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinSaleAmount { get; set; }

    [Range(0, 100)]
    public decimal MaxRedeemPercent { get; set; }

    /// <summary>null yoki 30..3650.</summary>
    public int? PointsExpireDays { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SignupBonus { get; set; }

    public bool EarnOnDiscountedItems { get; set; } = true;

    public List<UpdateLoyaltyCategoryRateRequest> CategoryRates { get; set; } = new();

    /// <summary>Darajalar — to'liq ro'yxat sifatida almashtiriladi. Null — o'zgarmaydi (eski klientlar uchun).</summary>
    public List<LoyaltyTierDto>? Tiers { get; set; }
}

public class UpdateLoyaltyCategoryRateRequest
{
    public Guid CategoryId { get; set; }

    [Range(0, 100)]
    public decimal Percent { get; set; }
}

/// <summary>Bonus tarixi elementi (mijoz ilovasi va admin panel uchun umumiy).</summary>
public class LoyaltyHistoryItemDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public LoyaltySource Source { get; set; }
    public decimal Points { get; set; }
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public string? ReceiptNumber { get; set; }
}

/// <summary>POST /api/loyalty/resolve so'rovi.</summary>
public class ResolveBonusCardRequest
{
    [Required]
    [MaxLength(512)]
    public string Code { get; set; } = string.Empty;
}

public class ResolveBonusCardResponse
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MaskedPhone { get; set; }
    public decimal Points { get; set; }
    public decimal MaxRedeemPercent { get; set; }

    /// <summary>Mijozning loyalty darajasi nomi (darajasi bo'lmasa null).</summary>
    public string? TierName { get; set; }
}

public class LoyaltyReportDto
{
    public decimal Earned { get; set; }
    public decimal Redeemed { get; set; }
    public decimal Expired { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal OutstandingPoints { get; set; }
}

/// <summary>GET /api/me/bonus-card javobi.</summary>
public class BonusCardDto
{
    public string StoreName { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public string QrPayload { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Joriy daraja (bo'lmasa null).</summary>
    public BonusTierDto? Tier { get; set; }

    /// <summary>Keyingi daraja va unga qolgan xarid summasi (eng yuqori darajada null).</summary>
    public BonusNextTierDto? NextTier { get; set; }
}

public class BonusTierDto
{
    public string Name { get; set; } = string.Empty;
    public decimal CashbackPercent { get; set; }
}

public class BonusNextTierDto
{
    public string Name { get; set; } = string.Empty;
    public decimal MinSpend { get; set; }
    public decimal Remaining { get; set; }
}

/// <summary>GET /api/me/bonus/stores elementi.</summary>
public class BonusStoreDto
{
    public Guid OrganizationId { get; set; }

    /// <summary>Birinchi faol online filial (bo'lmasa null).</summary>
    public Guid? BranchId { get; set; }

    public string StoreName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public decimal Points { get; set; }
}
