using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Loyalty.Dtos;

// Bosqich 10.2: keshbek sozlamalari tarixi, bonus harakatlari jurnali, qo'lda tuzatish.

/// <summary>{ items, total } — kelishuvdagi sahifalash shakli.</summary>
public class LoyaltyPageDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
}

/// <summary>GET /api/loyalty/settings/history elementi.</summary>
public class LoyaltySettingsChangeDto
{
    public DateTime ChangedAt { get; set; }
    public string? UserName { get; set; }
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class LoyaltyTransactionsQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public LoyaltyTransactionType? Type { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>GET /api/loyalty/transactions elementi.</summary>
public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Maskalangan telefon (+998 ** *** 45 67).</summary>
    public string? CustomerPhone { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public decimal Points { get; set; }

    /// <summary>Harakatdan keyingi balans (eski yozuvlarda null).</summary>
    public decimal? BalanceAfter { get; set; }
    public LoyaltySource Source { get; set; }
    public Guid? SaleId { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? OnlineOrderId { get; set; }
    public string? Note { get; set; }
    public string? UserName { get; set; }
}

public class LoyaltyTransactionTotalsDto
{
    /// <summary>Topilgan: Earn + Signup + Referral.</summary>
    public decimal Earned { get; set; }

    /// <summary>Ball bilan to'langan (musbat son).</summary>
    public decimal Redeemed { get; set; }

    /// <summary>Qaytarishlar bo'yicha sof harakat (manfiy — ayirilgan).</summary>
    public decimal Refunded { get; set; }

    /// <summary>Muddati o'tib yonganlar (musbat son).</summary>
    public decimal Expired { get; set; }

    /// <summary>Qo'lda tuzatishlar (sof, ishorali).</summary>
    public decimal Adjusted { get; set; }
}

public class LoyaltyTransactionsPageDto : LoyaltyPageDto<LoyaltyTransactionDto>
{
    public LoyaltyTransactionTotalsDto Totals { get; set; } = new();
}

/// <summary>POST /api/loyalty/customers/{customerId}/adjust</summary>
public class LoyaltyAdjustRequest
{
    /// <summary>Musbat — qo'shish, manfiy — ayirish (butun ball).</summary>
    public decimal Points { get; set; }

    [Required]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}

public class LoyaltyAdjustResultDto
{
    public Guid CustomerId { get; set; }

    /// <summary>Haqiqatda yozilgan ball (manfiyda balansgacha qisqartiriladi).</summary>
    public decimal Points { get; set; }

    /// <summary>Yangi balans.</summary>
    public decimal Balance { get; set; }
}
