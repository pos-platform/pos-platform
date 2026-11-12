using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopPush.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopPush;

public interface IShopPushService
{
    /// <summary>Qurilma tokenini ro'yxatdan o'tkazadi (upsert). Token boshqa akkauntda bo'lsa — shu akkauntga o'tkaziladi.</summary>
    Task RegisterDeviceAsync(Guid shopUserId, RegisterShopDeviceRequest request, CancellationToken ct = default);

    /// <summary>Faqat shu mijozning tokenini o'chiradi (boshqanikini emas).</summary>
    Task UnregisterDeviceAsync(Guid shopUserId, string token, CancellationToken ct = default);

    /// <summary>Buyurtma holati haqida mijozning barcha qurilmalariga push. Yaroqsiz tokenlar o'chiriladi.</summary>
    Task SendOrderStatusAsync(OrderStatusPush push, CancellationToken ct = default);
}

/// <summary>
/// Mijoz push bildirishnomalari. Mijoz global — tenant filtri o'chiq (IgnoreQueryFilters),
/// har so'rov ShopUserId bo'yicha qo'lda cheklanadi.
/// </summary>
public class ShopPushService : IShopPushService
{
    /// <summary>Bir mijozga ko'pi bilan shuncha qurilma (eng eskilari o'chiriladi).</summary>
    public const int MaxDevicesPerUser = 10;

    private static readonly string[] Platforms = ["android", "ios"];

    private readonly IApplicationDbContext _db;
    private readonly IPushSender _sender;
    private readonly ILogger<ShopPushService> _logger;

    public ShopPushService(IApplicationDbContext db, IPushSender sender, ILogger<ShopPushService> logger)
    {
        _db = db;
        _sender = sender;
        _logger = logger;
    }

    public async Task RegisterDeviceAsync(Guid shopUserId, RegisterShopDeviceRequest request, CancellationToken ct = default)
    {
        var token = request.Token.Trim();
        if (token.Length == 0)
            throw new ValidationException("Qurilma tokeni bo'sh.");

        var platform = request.Platform.Trim().ToLowerInvariant();
        if (!Platforms.Contains(platform))
            throw new ValidationException("Platforma noto'g'ri: android yoki ios.");

        var locale = NormalizeLocale(request.Locale);
        var now = DateTime.UtcNow;

        var userExists = await _db.ShopUsers.IgnoreQueryFilters()
            .AnyAsync(u => u.Id == shopUserId && u.IsActive && u.DeletedAt == null, ct);
        if (!userExists)
            throw new UnauthorizedException("Akkaunt topilmadi.");

        var device = await _db.ShopDeviceTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Token == token, ct);

        if (device is null)
        {
            device = new ShopDeviceToken { ShopUserId = shopUserId, Token = token };
            _db.ShopDeviceTokens.Add(device);
        }

        // Boshqa akkaunt shu qurilmadan kirgan bo'lsa — token yangi egaga o'tadi.
        device.ShopUserId = shopUserId;
        device.Platform = platform;
        device.Locale = locale;
        device.LastSeenAt = now;
        device.OrganizationId = null;

        // Limit: eng eski qurilmalarni o'chiramiz.
        var others = await _db.ShopDeviceTokens.IgnoreQueryFilters()
            .Where(d => d.ShopUserId == shopUserId && d.Token != token)
            .OrderByDescending(d => d.LastSeenAt)
            .Skip(MaxDevicesPerUser - 1)
            .ToListAsync(ct);
        _db.ShopDeviceTokens.RemoveRange(others);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Bir vaqtda ikki so'rov bir xil tokenni qo'shdi — unikal indeks. Ilova keyingi ochilishda qayta yuboradi.
            throw new ConflictException("Qurilma bir vaqtda ro'yxatdan o'tkazilmoqda. Qayta urinib ko'ring.");
        }
    }

    public async Task UnregisterDeviceAsync(Guid shopUserId, string token, CancellationToken ct = default)
    {
        token = (token ?? string.Empty).Trim();
        if (token.Length == 0) return;

        var devices = await _db.ShopDeviceTokens.IgnoreQueryFilters()
            .Where(d => d.ShopUserId == shopUserId && d.Token == token)
            .ToListAsync(ct);
        if (devices.Count == 0) return;

        _db.ShopDeviceTokens.RemoveRange(devices);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SendOrderStatusAsync(OrderStatusPush push, CancellationToken ct = default)
    {
        if (OrderPushTexts.Build(push, "uz") is null) return; // bu holat uchun push yuborilmaydi

        var devices = await _db.ShopDeviceTokens.IgnoreQueryFilters().AsNoTracking()
            .Where(d => d.ShopUserId == push.ShopUserId)
            .Select(d => new { d.Id, d.Token, d.Locale })
            .ToListAsync(ct);
        if (devices.Count == 0) return;

        var data = new Dictionary<string, string>
        {
            ["type"] = "order",
            ["orderId"] = push.OrderId.ToString(),
            ["status"] = push.Status.ToString()
        };

        var invalid = new List<Guid>();
        foreach (var device in devices)
        {
            var text = OrderPushTexts.Build(push, device.Locale)!.Value;
            var result = await _sender.SendAsync(device.Token, text.Title, text.Body, data, ct);
            if (result == PushSendResult.InvalidToken) invalid.Add(device.Id);
        }

        if (invalid.Count > 0)
        {
            var stale = await _db.ShopDeviceTokens.IgnoreQueryFilters()
                .Where(d => invalid.Contains(d.Id))
                .ToListAsync(ct);
            _db.ShopDeviceTokens.RemoveRange(stale);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Yaroqsiz push tokenlar o'chirildi: {Count}", invalid.Count);
        }
    }

    public static string NormalizeLocale(string? locale) =>
        (locale ?? string.Empty).Trim().ToLowerInvariant().StartsWith("ru") ? "ru" : "uz";
}

/// <summary>Buyurtma holati bo'yicha push matnlari (uz/ru).</summary>
public static class OrderPushTexts
{
    /// <returns>null — bu holat uchun push yuborilmaydi (New).</returns>
    public static (string Title, string Body)? Build(OrderStatusPush p, string? locale)
    {
        var ru = ShopPushService.NormalizeLocale(locale) == "ru";
        var n = p.Number;
        var pickup = p.FulfillmentType == FulfillmentType.Pickup;
        var reason = string.IsNullOrWhiteSpace(p.Reason) ? null : p.Reason.Trim();
        // Kuryer: "Ali, +998901234567" (bor qismi bilan).
        var courier = string.Join(", ", new[] { p.CourierName, p.CourierPhone }
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

        return p.Status switch
        {
            OnlineOrderStatus.Accepted => ru
                ? ($"Заказ #{n} принят", "Магазин подтвердил ваш заказ.")
                : ($"Buyurtma #{n} qabul qilindi", "Do'kon buyurtmangizni tasdiqladi."),
            OnlineOrderStatus.Picking => ru
                ? ($"Заказ #{n} собирается", "Мы собираем ваши товары.")
                : ($"Buyurtma #{n} yig'ilmoqda", "Mahsulotlaringiz yig'ilmoqda."),
            OnlineOrderStatus.Ready => ru
                ? ($"Заказ #{n} готов", "Скоро передадим его курьеру.")
                : ($"Buyurtma #{n} tayyor", "Tez orada kuryerga topshiriladi."),
            OnlineOrderStatus.ReadyForPickup => ru
                ? ($"Заказ #{n} готов к выдаче", "Можете забрать его в магазине.")
                : ($"Buyurtma #{n} olib ketishga tayyor", "Do'kondan olib ketishingiz mumkin."),
            OnlineOrderStatus.AwaitingTransfer => ru
                ? ($"Заказ #{n}: ожидается перевод", "Оплатите переводом — номер карты в приложении.")
                : ($"Buyurtma #{n}: o'tkazma kutilmoqda", "To'lovni o'tkazing — karta raqami ilovada."),
            OnlineOrderStatus.OnTheWay => ru
                ? ($"Заказ #{n} в пути", courier.Length > 0
                    ? $"Курьер уже везёт ваш заказ. Курьер: {courier}."
                    : "Курьер уже везёт ваш заказ.")
                : ($"Buyurtma #{n} yo'lda", courier.Length > 0
                    ? $"Kuryer buyurtmangizni olib kelmoqda. Kuryer: {courier}."
                    : "Kuryer buyurtmangizni olib kelmoqda."),
            OnlineOrderStatus.Delivered => ru
                ? (pickup ? $"Заказ #{n} выдан" : $"Заказ #{n} доставлен", "Спасибо за покупку!")
                : (pickup ? $"Buyurtma #{n} topshirildi" : $"Buyurtma #{n} yetkazildi", "Xaridingiz uchun rahmat!"),
            OnlineOrderStatus.Rejected => ru
                ? ($"Заказ #{n} отклонён", reason ?? "Подробности в приложении.")
                : ($"Buyurtma #{n} rad etildi", reason ?? "Batafsil ma'lumot ilovada."),
            OnlineOrderStatus.Cancelled => ru
                ? ($"Заказ #{n} отменён магазином", reason is null ? "Подробности в приложении." : $"Причина: {reason}")
                : ($"Buyurtma #{n} do'kon tomonidan bekor qilindi", reason is null ? "Batafsil ma'lumot ilovada." : $"Sabab: {reason}"),
            _ => null
        };
    }
}
