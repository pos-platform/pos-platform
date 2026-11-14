using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Musbat = topilgan ball, manfiy = sarflangan ball.</summary>
    public decimal Points { get; set; }

    /// <summary>Ball hisoblangan savdo summasi.</summary>
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    /// <summary>Harakat turi (eski yozuvlar: musbat → Earn, manfiy → Redeem).</summary>
    public LoyaltyTransactionType Type { get; set; } = LoyaltyTransactionType.Earn;

    /// <summary>Manba: kassa yoki mobil ilova.</summary>
    public LoyaltySource Source { get; set; } = LoyaltySource.Store;

    /// <summary>Harakatdan keyingi balans (eski yozuvlarda null).</summary>
    public decimal? BalanceAfter { get; set; }

    /// <summary>Harakatni bajargan xodim (qo'lda tuzatish, qaytarish). Avtomatik harakatlarda null.</summary>
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
}
