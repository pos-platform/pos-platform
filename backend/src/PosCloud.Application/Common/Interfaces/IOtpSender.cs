namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Kirish kodini mijozga yetkazadi. Implementatsiya avval Telegram Gateway, bo'lmasa SMS orqali yuboradi.
/// Hech bir kanal ishlamasa exception tashlaydi.
/// </summary>
public interface IOtpSender
{
    /// <param name="phone">+998XXXXXXXXX</param>
    /// <param name="code">Raqamli kod.</param>
    /// <param name="ttlSeconds">Kod amal qilish muddati.</param>
    /// <param name="preferSms">Mijoz "SMS orqali yuborish"ni tanlagan bo'lsa Telegram o'tkazib yuboriladi.</param>
    /// <returns>Yuborilgan kanal: telegram, sms yoki log.</returns>
    Task<string> SendAsync(string phone, string code, int ttlSeconds, bool preferSms, CancellationToken ct = default);
}
