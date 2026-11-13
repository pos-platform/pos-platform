using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopMarketing;

/// <summary>
/// Do'stni taklif qilish mukofoti: taklif qilingan mijozning shu tashkilotdagi BIRINCHI buyurtmasi Delivered bo'lganda
/// ikkalasiga ham loyalty ball (Referral). Xodim (OnlineOrderService) va kuryer (CourierService) Delivered yo'llari chaqiradi.
/// Idempotent: ShopReferral.RewardedAt qo'yiladi. SaveChanges chaqirmaydi — push'lar saqlangandan keyin navbatga qo'yilsin.
/// </summary>
public static class ShopReferralRewards
{
    public static async Task<IReadOnlyList<ShopUserPush>> RewardOnDeliveredAsync(IApplicationDbContext db, OnlineOrder order,
        CancellationToken ct)
    {
        if (order.OrganizationId is not { } orgId) return [];

        var referral = await db.ShopReferrals.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == orgId && r.RefereeShopUserId == order.ShopUserId
                                      && r.RewardedAt == null, ct);
        if (referral is null) return [];

        // Faqat birinchi yetkazilgan buyurtma.
        var hadDelivered = await db.OnlineOrders.IgnoreQueryFilters()
            .AnyAsync(o => o.OrganizationId == orgId && o.ShopUserId == order.ShopUserId && o.Id != order.Id
                           && o.Status == OnlineOrderStatus.Delivered, ct);
        if (hadDelivered) return [];

        var users = await db.ShopUsers.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == referral.ReferrerShopUserId || u.Id == referral.RefereeShopUserId)
            .ToListAsync(ct);
        var referrer = users.FirstOrDefault(u => u.Id == referral.ReferrerShopUserId);
        var referee = users.FirstOrDefault(u => u.Id == referral.RefereeShopUserId);

        referral.RewardedAt = DateTime.UtcNow;
        var pushes = new List<ShopUserPush>();

        if (referee is not null && referral.RefereeBonus > 0)
        {
            var customer = await ShopCustomerLinker.EnsureAsync(db, orgId, referee, LoyaltySource.Online, ct);
            var points = LoyaltyRules.Apply(db, customer, referral.RefereeBonus, LoyaltyTransactionType.Referral,
                LoyaltySource.Online, 0, $"Taklif kodi bonusi (buyurtma #{order.Number})");
            if (points > 0) pushes.Add(Push(referee.Id, orgId, points, false));
        }

        if (referrer is not null && referrer.IsActive && referrer.DeletedAt is null && referral.ReferrerBonus > 0)
        {
            var customer = await ShopCustomerLinker.EnsureAsync(db, orgId, referrer, LoyaltySource.Online, ct);
            var points = LoyaltyRules.Apply(db, customer, referral.ReferrerBonus, LoyaltyTransactionType.Referral,
                LoyaltySource.Online, 0, "Do'stni taklif qilganingiz uchun bonus");
            if (points > 0) pushes.Add(Push(referrer.Id, orgId, points, true));
        }

        return pushes;
    }

    private static ShopUserPush Push(Guid shopUserId, Guid orgId, decimal points, bool isReferrer) => new(
        shopUserId,
        isReferrer ? "Do'stingiz xarid qildi!" : "Taklif bonusi",
        $"+{points:0} bonus ball hisobingizga qo'shildi.",
        isReferrer ? "Ваш друг сделал покупку!" : "Бонус за приглашение",
        $"+{points:0} бонусных баллов зачислено на ваш счёт.",
        new Dictionary<string, string>
        {
            ["type"] = "referral_reward",
            ["organizationId"] = orgId.ToString()
        });
}
