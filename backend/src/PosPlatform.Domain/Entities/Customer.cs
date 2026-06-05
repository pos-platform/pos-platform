using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>Mijoz (xaridor) — sodiqlik dasturi uchun.</summary>
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>To'plangan ball (1 ball = 1 so'm chegirma).</summary>
    public decimal LoyaltyPoints { get; set; }

    /// <summary>Chegirma foizi (0–100). Baldan ajratilgan doimiy chegirma.</summary>
    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Comment { get; set; }

    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
