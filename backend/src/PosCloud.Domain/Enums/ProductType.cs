namespace PosCloud.Domain.Enums;

/// <summary>
/// Mahsulot turi. Restoran rejimida ovqat (Dish) sotuvda ombordan ayirilmaydi —
/// xom-ashyo (Good) faqat "ombordan oshpazga berish" (KitchenIssue) orqali kamayadi.
/// </summary>
public enum ProductType
{
    /// <summary>Tovar / xom-ashyo (ombor qoldig'i bor).</summary>
    Good = 0,

    /// <summary>Ovqat (taom) — sotuvda, retsept orqali tovarlardan tayyorlanadi.</summary>
    Dish = 1,

    /// <summary>Xizmat (masalan, yetkazib berish) — ombordan ayirilmaydi, POS/katalog ro'yxatlarida ko'rinmaydi.</summary>
    Service = 2
}
