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

    // ---- Bosqich 10 ----

    /// <summary>
    /// Qaytarish so'rovi: xodimga <c>ReturnCreated</c>/<c>ReturnUpdated</c>, mijozga <c>ReturnStatusChanged</c>
    /// (payload <c>{ id, number, branchId, status }</c>), biriktirilgan kuryerga <c>ReturnUpdated</c>.
    /// pushToCustomer — push <c>type=return_status { returnId }</c>.
    /// </summary>
    Task ReturnChangedAsync(OnlineReturn ret, bool isNew, bool pushToCustomer, CancellationToken ct = default);

    /// <summary>Kuryerga yangi taklif: <c>courier:{id}</c> guruhiga <c>CourierOffer</c> (<c>{ offerId, orderId, number, totalAmount, expiresAt }</c>).</summary>
    Task CourierOfferAsync(Guid courierUserId, CourierOfferEvent evt, CancellationToken ct = default);

    /// <summary>Taklif yopildi (boshqa kuryer oldi yoki bekor qilindi): <c>CourierOfferClosed { orderId }</c>.</summary>
    Task CourierOfferClosedAsync(Guid orderId, IReadOnlyCollection<Guid> courierUserIds, CancellationToken ct = default);
}

/// <summary>SignalR <c>CourierOffer</c> xabari (shaxsiy ma'lumotsiz).</summary>
public sealed record CourierOfferEvent(Guid OfferId, Guid OrderId, int Number, decimal TotalAmount, DateTime ExpiresAt);
