using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Musbat = topilgan ball, manfiy = sarflangan ball.</summary>
    public decimal Points { get; set; }

    /// <summary>Ball hisoblangan savdo summasi.</summary>
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
}
