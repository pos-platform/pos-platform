namespace PosPlatform.Application.Features.Products.Dtos;

/// <summary>
/// Kassa katalogining "tamg'asi" (stamp) — kassa shu yengil javob orqali "ro'yxatni
/// qayta yuklash kerakmi?" degan savolga javob oladi (to'liq ro'yxat o'rniga ~100 bayt).
///
/// Soni va vaqti birgalikda barcha o'zgarishni ushlaydi:
///   Count        — qo'shish va o'chirishni (mahsulot hard delete qilinadi, tombstone yo'q),
///   UpdatedAt    — tahrirni (narx, nom, MXIK).
/// Yangi yozuvda UpdatedAt null bo'lgani uchun CreatedAt bilan almashtiriladi.
/// </summary>
public class PosCatalogStampDto
{
    /// <summary>Kassaga ko'rinadigan mahsulotlar soni.</summary>
    public int ProductCount { get; set; }

    /// <summary>Mahsulot kartochkasi (narx, nom, MXIK) oxirgi marta o'zgargan vaqt (UTC).</summary>
    public DateTime? ProductsUpdatedAt { get; set; }

    /// <summary>
    /// Qoldiq (BranchStock) oxirgi marta o'zgargan vaqt (UTC). Kartochkadan ALOHIDA turadi:
    /// qoldiq har savdoda o'zgaradi, shu sabab kassa uni siyrakroq yangilaydi va kassirga
    /// bu haqda xabar ko'rsatmaydi — narx o'zgarishi bilan bir xil muomala qilib bo'lmaydi.
    /// </summary>
    public DateTime? StocksUpdatedAt { get; set; }

    /// <summary>Kategoriyalar soni.</summary>
    public int CategoryCount { get; set; }

    /// <summary>Kategoriya oxirgi marta o'zgargan vaqt (UTC). null = kategoriya yo'q.</summary>
    public DateTime? CategoriesUpdatedAt { get; set; }
}
