namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Mijozga (ShopUser) umumiy push xabari: matn uz/ru, data — ilova navigatsiyasi uchun (type va boshqalar).
/// </summary>
public sealed record ShopUserPush(Guid ShopUserId, string TitleUz, string BodyUz, string TitleRu, string BodyRu,
    IReadOnlyDictionary<string, string> Data);

/// <summary>
/// Mijoz push'lari navbati (Api: kanal + fon vazifa). So'rov ichida faqat navbatga qo'yiladi — FCM'ni kutmaydi.
/// Navbat to'lsa xabar tashlab yuboriladi (logga yoziladi).
/// </summary>
public interface IShopPushQueue
{
    void Enqueue(ShopUserPush push);
}
