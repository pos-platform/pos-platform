using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>Qarzdor (Должник) — nasiyaga olgan mijoz. Balans = bizga qarzi.</summary>
public class Debtor : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Comment { get; set; }

    /// <summary>Joriy qarz (musbat = bizga qarzdor).</summary>
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DebtorTransaction> Transactions { get; set; } = new List<DebtorTransaction>();
}
