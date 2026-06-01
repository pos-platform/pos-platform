using PosPlatform.Domain.Common;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Kassa smenasi (sessiyasi). Kassir smenani boshlang'ich naqd bilan ochadi,
/// savdolar shu smenaga bog'lanadi, yopishda naqd sanab hisobot olinadi.
/// </summary>
public class CashShift : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Smenani ochgan kassir.</summary>
    public Guid CashierId { get; set; }
    public User? Cashier { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    /// <summary>Smena boshidagi naqd (kassadagi pul).</summary>
    public decimal OpeningCash { get; set; }

    // --- Yopishdagi snapshot (Status=Closed bo'lganda to'ldiriladi) ---

    /// <summary>Yopishda sanab chiqilgan haqiqiy naqd.</summary>
    public decimal? CountedCash { get; set; }

    /// <summary>Sotuvlar yig'indisi (refundsiz).</summary>
    public decimal TotalSales { get; set; }

    /// <summary>Qaytarishlar (refund) yig'indisi.</summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>Naqd sotuvlar (refund ayrilgan) — kassada kutilgan naqd o'sishi.</summary>
    public decimal CashSalesNet { get; set; }

    /// <summary>Karta orqali sotuvlar (refund ayrilgan).</summary>
    public decimal CardSalesNet { get; set; }

    /// <summary>Kutilgan naqd = OpeningCash + CashSalesNet.</summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>Farq = CountedCash - ExpectedCash (manfiy = kam, musbat = ortiq).</summary>
    public decimal CashDifference { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
