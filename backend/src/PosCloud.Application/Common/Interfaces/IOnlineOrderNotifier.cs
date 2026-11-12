using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Online buyurtma o'zgarishlari haqida real vaqt xabari (SignalR, /hubs/orders).
/// Xodimlarga — filial/tashkilot guruhiga, mijozga — o'z guruhiga. Xabar yengil: klient tafsilotni API'dan oladi.
/// Xabar yuborilmasa ham asosiy amal muvaffaqiyatli hisoblanadi (implementatsiya xatoni yutadi va loglaydi).
/// </summary>
public interface IOnlineOrderNotifier
{
    /// <param name="pushToCustomer">
    /// true — do'kon holatni o'zgartirdi, mijozga push bildirishnoma ham yuboriladi (navbat orqali, so'rovni kutdirmaydi).
    /// Yangi buyurtma, mijozning o'z amali (bekor qilish, chek yuklash) yoki holat o'zgarmagan bo'lsa — false.
    /// </param>
    Task OrderChangedAsync(OnlineOrder order, bool isNew, bool pushToCustomer, CancellationToken ct = default);

    /// <summary>Buyurtma kuryerga biriktirildi — kuryer guruhiga (<c>courier:{userId}</c>) <c>CourierOrderAssigned</c>.</summary>
    Task CourierAssignedAsync(OnlineOrder order, CancellationToken ct = default);
}
