using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities;

/// <summary>
/// Aksiya (chegirma). Ma'lum mahsulot, kategoriya yoki barcha mahsulotlarga
/// foizli chegirma beradi. Faqat sana oralig'ida va IsActive bo'lganda amal qiladi.
/// POS savdoda avtomatik qo'llanadi.
/// </summary>
public class Promotion : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Chegirma foizi (0–100).</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Aniq mahsulotga (null bo'lsa — mahsulotga emas).</summary>
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Kategoriyaga (null bo'lsa — kategoriyaga emas).</summary>
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>ProductId va CategoryId ham null bo'lsa — barcha mahsulotlarga.</summary>
    public bool AppliesToAll => ProductId is null && CategoryId is null;
}
