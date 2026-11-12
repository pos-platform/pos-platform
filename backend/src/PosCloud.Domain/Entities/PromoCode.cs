using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Online buyurtma promokodi (tashkilot bo'yicha). Foiz yoki qat'iy summa (bittasi).
/// Buyurtma yaratilganda UsedCount oshadi, bekor/rad etilganda qaytariladi.
/// </summary>
public class PromoCode : BaseEntity
{
    /// <summary>Katta harflarda saqlanadi, tashkilot ichida unikal.</summary>
    public string Code { get; set; } = string.Empty;

    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }

    /// <summary>Mahsulotlar summasi kamida shuncha bo'lsa amal qiladi.</summary>
    public decimal MinOrderAmount { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary>Jami foydalanish limiti. Null — cheklovsiz.</summary>
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }

    /// <summary>Bitta mijoz necha marta ishlatishi mumkin. 0 — cheklovsiz.</summary>
    public int PerUserLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
