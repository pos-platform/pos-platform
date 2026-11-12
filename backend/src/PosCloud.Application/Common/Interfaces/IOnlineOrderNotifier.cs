using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Online buyurtma o'zgarishlari haqida real vaqt xabari (SignalR, /hubs/orders).
/// Xodimlarga — filial/tashkilot guruhiga, mijozga — o'z guruhiga. Xabar yengil: klient tafsilotni API'dan oladi.
/// Xabar yuborilmasa ham asosiy amal muvaffaqiyatli hisoblanadi (implementatsiya xatoni yutadi va loglaydi).
/// </summary>
public interface IOnlineOrderNotifier
{
    Task OrderChangedAsync(OnlineOrder order, bool isNew, CancellationToken ct = default);
}
