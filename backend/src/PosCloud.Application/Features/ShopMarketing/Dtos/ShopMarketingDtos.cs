using System.ComponentModel.DataAnnotations;

namespace PosCloud.Application.Features.ShopMarketing.Dtos;

/// <summary>PUT /api/me/stock-subscriptions so'rovi.</summary>
public class StockSubscriptionRequest
{
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
}

public class ShopCartItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>PUT /api/me/cart — savatcha nusxasi (bo'sh items — o'chiradi).</summary>
public class SaveShopCartRequest
{
    public Guid BranchId { get; set; }

    [MaxLength(100)]
    public List<ShopCartItemRequest> Items { get; set; } = [];
}

/// <summary>GET /api/me/referral javobi.</summary>
public class ShopReferralDto
{
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public decimal ReferrerBonus { get; set; }
    public decimal RefereeBonus { get; set; }
    public int InvitedCount { get; set; }
    public int RewardedCount { get; set; }
    public string ShareText { get; set; } = string.Empty;
}

public class ApplyReferralRequest
{
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;
}
