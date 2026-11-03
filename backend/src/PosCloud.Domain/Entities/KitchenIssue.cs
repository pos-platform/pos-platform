using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Ombordan oshpazga berish hujjati. Tanlangan ovqatlar uchun tovarlar (xom-ashyo)
/// oshpazga beriladi va filial omboridan ayiriladi. Hisobot uchun: qaysi ovqatga
/// qancha tovar sarflangani kuzatiladi. (WriteOff andozasiga o'xshash.)
/// </summary>
public class KitchenIssue : BaseEntity
{
    public string Number { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Tovar berilgan oshpaz (User, ixtiyoriy).</summary>
    public Guid? ChefUserId { get; set; }
    public User? ChefUser { get; set; }

    public string? Comment { get; set; }

    /// <summary>Berilgan tovarlarning umumiy tannarxi.</summary>
    public decimal TotalCost { get; set; }

    public ICollection<KitchenIssueItem> Items { get; set; } = new List<KitchenIssueItem>();
}
