using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;
using ValidationException = PosCloud.Application.Common.Exceptions.ValidationException;

namespace PosCloud.Application.Features.Courier;

// Bosqich 10.6: kuryerga taklif (broadcast), birinchi qabul qilgan oladi.

/// <summary>GET /api/courier/offers elementi.</summary>
public class CourierOfferDto
{
    public Guid OfferId { get; set; }
    public Guid OrderId { get; set; }
    public int Number { get; set; }
    public string? BranchName { get; set; }
    public string? Address { get; set; }
    public double? DistanceKm { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public int ItemCount { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Xodim tafsilotidagi taklif tarixi elementi.</summary>
public class CourierOfferHistoryDto
{
    public string CourierName { get; set; } = string.Empty;
    public CourierOfferItemStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class CourierDeviceTokenRequest
{
    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    /// <summary>"android" yoki "ios".</summary>
    [MaxLength(10)]
    public string? Platform { get; set; }
}

public class CourierDeclineOfferRequest
{
    [MaxLength(300)]
    public string? Reason { get; set; }
}

public interface ICourierOfferService
{
    /// <summary>Tayyor yetkazish buyurtmasi uchun filial kuryerlariga taklif (avtomatik — faqat Broadcast rejimida).</summary>
    Task<int> SendAsync(OnlineOrder order, bool manual, CancellationToken ct = default);

    /// <summary>Buyurtmaning kutilayotgan takliflarini bekor qiladi (qo'lda biriktirildi yoki buyurtma bekor). SaveChanges chaqirmaydi.</summary>
    Task<IReadOnlyList<Guid>> CancelPendingAsync(Guid orderId, CancellationToken ct = default);

    Task<IReadOnlyList<CourierOfferDto>> GetMineAsync(CancellationToken ct = default);
    Task<OnlineOrder> AcceptAsync(Guid offerId, CancellationToken ct = default);
    Task DeclineAsync(Guid offerId, string? reason, CancellationToken ct = default);
    Task<IReadOnlyList<CourierOfferHistoryDto>> GetHistoryAsync(Guid orderId, CancellationToken ct = default);

    Task RegisterDeviceAsync(CourierDeviceTokenRequest request, CancellationToken ct = default);
    Task UnregisterDeviceAsync(string token, CancellationToken ct = default);

    /// <summary>Fon vazifasi: muddati o'tgan takliflar Expired; kutilayotgani qolmagan buyurtma — NoCourier.</summary>
    Task<int> ExpireDueAsync(DateTime nowUtc, CancellationToken ct = default);
}

public class CourierOfferService : ICourierOfferService
{
    public const string TakenMessage = "Buyurtmani boshqa kuryer oldi";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IOnlineOrderNotifier _notifier;
    private readonly IPushSender _push;
    private readonly IShopPushQueue _shopPush;
    private readonly ILogger<CourierOfferService> _logger;

    public CourierOfferService(IApplicationDbContext db, ICurrentUser currentUser, IOnlineOrderNotifier notifier,
        IPushSender push, IShopPushQueue shopPush, ILogger<CourierOfferService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _push = push;
        _shopPush = shopPush;
        _logger = logger;
    }

    public async Task<int> SendAsync(OnlineOrder order, bool manual, CancellationToken ct = default)
    {
        if (order.FulfillmentType != FulfillmentType.Delivery || order.Status != OnlineOrderStatus.Ready
            || order.CourierUserId is not null)
        {
            if (manual) throw new ValidationException("Taklif faqat kuryer biriktirilmagan, tayyor yetkazish buyurtmasiga yuboriladi.");
            return 0;
        }

        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == order.OrganizationId, ct);
        if (!manual && (settings?.CourierOfferMode ?? CourierOfferMode.Broadcast) == CourierOfferMode.Off) return 0;
        var timeout = Math.Clamp(settings?.CourierOfferTimeoutSec ?? 120, 15, 3600);

        var closed = await CancelPendingAsync(order.Id, ct);

        var couriers = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.OrganizationId == order.OrganizationId && u.Role == UserRole.Courier && u.IsActive
                        && u.BranchId == order.BranchId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var offers = couriers.Select(c => new CourierOffer
        {
            OrganizationId = order.OrganizationId,
            OnlineOrderId = order.Id,
            CourierUserId = c,
            Status = CourierOfferItemStatus.Pending,
            ExpiresAt = now.AddSeconds(timeout)
        }).ToList();
        _db.CourierOffers.AddRange(offers);
        order.CourierOfferStatus = offers.Count > 0 ? CourierOfferStatus.Pending : CourierOfferStatus.NoCourier;
        await _db.SaveChangesAsync(ct);

        if (closed.Count > 0) await _notifier.CourierOfferClosedAsync(order.Id, closed, ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        foreach (var offer in offers)
            await _notifier.CourierOfferAsync(offer.CourierUserId,
                new CourierOfferEvent(offer.Id, order.Id, order.Number, order.TotalAmount, offer.ExpiresAt), ct);

        await PushOffersAsync(order, offers, ct);
        return offers.Count;
    }

    public async Task<IReadOnlyList<Guid>> CancelPendingAsync(Guid orderId, CancellationToken ct = default)
    {
        var pending = await _db.CourierOffers.IgnoreQueryFilters()
            .Where(o => o.OnlineOrderId == orderId && o.Status == CourierOfferItemStatus.Pending)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var o in pending)
        {
            o.Status = CourierOfferItemStatus.Cancelled;
            o.RespondedAt = now;
        }
        return pending.Select(o => o.CourierUserId).ToList();
    }

    public async Task<IReadOnlyList<CourierOfferDto>> GetMineAsync(CancellationToken ct = default)
    {
        var me = RequireCourierId();
        var now = DateTime.UtcNow;
        var rows = await _db.CourierOffers.AsNoTracking()
            .Where(o => o.CourierUserId == me && o.Status == CourierOfferItemStatus.Pending && o.ExpiresAt > now
                        && o.OnlineOrder!.Status == OnlineOrderStatus.Ready && o.OnlineOrder.CourierUserId == null)
            .OrderBy(o => o.ExpiresAt)
            .Select(o => new
            {
                o.Id,
                o.OnlineOrderId,
                o.OnlineOrder!.Number,
                BranchName = o.OnlineOrder.Branch!.Name,
                BranchLat = o.OnlineOrder.Branch.Latitude,
                BranchLng = o.OnlineOrder.Branch.Longitude,
                o.OnlineOrder.AddressLine,
                o.OnlineOrder.Latitude,
                o.OnlineOrder.Longitude,
                o.OnlineOrder.TotalAmount,
                o.OnlineOrder.PaymentMethod,
                ItemCount = o.OnlineOrder.Items.Count(i => !i.IsRemoved),
                o.ExpiresAt
            })
            .ToListAsync(ct);

        return rows.Select(r => new CourierOfferDto
        {
            OfferId = r.Id,
            OrderId = r.OnlineOrderId,
            Number = r.Number,
            BranchName = r.BranchName,
            Address = r.AddressLine,
            DistanceKm = r.BranchLat is { } bla && r.BranchLng is { } blo && r.Latitude is { } la && r.Longitude is { } lo
                ? Math.Round(ShopCatalogService.HaversineKm(bla, blo, la, lo), 1)
                : null,
            TotalAmount = r.TotalAmount,
            PaymentMethod = r.PaymentMethod,
            ItemCount = r.ItemCount,
            ExpiresAt = r.ExpiresAt
        }).ToList();
    }

    public async Task<OnlineOrder> AcceptAsync(Guid offerId, CancellationToken ct = default)
    {
        var me = RequireCourierId();
        var courier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == me, ct)
                      ?? throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        var offer = await _db.CourierOffers.FirstOrDefaultAsync(o => o.Id == offerId && o.CourierUserId == me, ct)
                    ?? throw new NotFoundException("Taklif topilmadi.");
        var order = await _db.OnlineOrders.Include(o => o.Branch).Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == offer.OnlineOrderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");

        if (order.CourierUserId == me) return order; // takroriy bosish
        if (order.CourierUserId is not null || order.Status != OnlineOrderStatus.Ready
            || offer.Status is CourierOfferItemStatus.Cancelled or CourierOfferItemStatus.Declined)
            throw new ConflictException(TakenMessage);
        if (offer.Status != CourierOfferItemStatus.Pending || offer.ExpiresAt < DateTime.UtcNow)
            throw new ConflictException("Taklif muddati tugagan.");

        var now = DateTime.UtcNow;
        order.CourierUserId = me;
        order.CourierName = Cut(CourierRules.DisplayName(courier), 100);
        order.CourierPhone = Cut(courier.Phone, 20);
        order.CourierAcceptedAt = now;
        order.CourierOfferStatus = CourierOfferStatus.Accepted;
        offer.Status = CourierOfferItemStatus.Accepted;
        offer.RespondedAt = now;

        var others = await _db.CourierOffers
            .Where(o => o.OnlineOrderId == order.Id && o.Id != offer.Id && o.Status == CourierOfferItemStatus.Pending)
            .ToListAsync(ct);
        foreach (var o in others)
        {
            o.Status = CourierOfferItemStatus.Cancelled;
            o.RespondedAt = now;
        }

        try
        {
            // CourierUserId — concurrency token: UPDATE ... WHERE CourierUserId IS NULL. Ikkinchi kuryer 409 oladi.
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(TakenMessage);
        }

        await _notifier.CourierOfferClosedAsync(order.Id, others.Select(o => o.CourierUserId).ToList(), ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        try
        {
            var name = order.CourierName ?? "Kuryer";
            _shopPush.Enqueue(new ShopUserPush(order.ShopUserId,
                $"Buyurtma #{order.Number}", $"Kuryer {name} buyurtmangizni oladi",
                $"Заказ #{order.Number}", $"Курьер {name} заберёт ваш заказ",
                new Dictionary<string, string> { ["type"] = "order", ["orderId"] = order.Id.ToString(), ["status"] = order.Status.ToString() }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mijozga kuryer push'i navbatga qo'yilmadi: {OrderId}", order.Id);
        }
        return order;
    }

    public async Task DeclineAsync(Guid offerId, string? reason, CancellationToken ct = default)
    {
        var me = RequireCourierId();
        var offer = await _db.CourierOffers.FirstOrDefaultAsync(o => o.Id == offerId && o.CourierUserId == me, ct)
                    ?? throw new NotFoundException("Taklif topilmadi.");
        if (offer.Status != CourierOfferItemStatus.Pending) return;

        offer.Status = CourierOfferItemStatus.Declined;
        offer.RespondedAt = DateTime.UtcNow;
        offer.DeclineReason = string.IsNullOrWhiteSpace(reason) ? null : Cut(reason.Trim(), 300);

        var order = await _db.OnlineOrders.FirstOrDefaultAsync(o => o.Id == offer.OnlineOrderId, ct);
        var anyPending = await _db.CourierOffers.AnyAsync(o => o.OnlineOrderId == offer.OnlineOrderId
                                                               && o.Id != offer.Id && o.Status == CourierOfferItemStatus.Pending, ct);
        if (order is not null && !anyPending && order.CourierUserId is null && order.CourierOfferStatus == CourierOfferStatus.Pending)
            order.CourierOfferStatus = CourierOfferStatus.NoCourier;

        await _db.SaveChangesAsync(ct);
        if (order is not null) await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
    }

    public async Task<IReadOnlyList<CourierOfferHistoryDto>> GetHistoryAsync(Guid orderId, CancellationToken ct = default) =>
        await _db.CourierOffers.AsNoTracking()
            .Where(o => o.OnlineOrderId == orderId)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new CourierOfferHistoryDto
            {
                CourierName = o.CourierUser!.FullName ?? o.CourierUser.Username,
                Status = o.Status,
                RespondedAt = o.RespondedAt
            })
            .ToListAsync(ct);

    public async Task RegisterDeviceAsync(CourierDeviceTokenRequest request, CancellationToken ct = default)
    {
        var me = RequireCourierId();
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > 512) throw new ValidationException("Token noto'g'ri.");
        var platform = (request.Platform ?? "android").Trim().ToLowerInvariant();
        if (platform is not ("android" or "ios")) platform = "android";

        // Token unikal: boshqa foydalanuvchida bo'lsa — shu kuryerga o'tkaziladi (qurilma almashgan).
        var row = await _db.StaffDeviceTokens.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Token == token, ct);
        if (row is null)
        {
            _db.StaffDeviceTokens.Add(new StaffDeviceToken
            {
                OrganizationId = _currentUser.OrganizationId,
                UserId = me,
                Token = token,
                Platform = platform
            });
        }
        else
        {
            row.UserId = me;
            row.OrganizationId = _currentUser.OrganizationId;
            row.Platform = platform;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnregisterDeviceAsync(string token, CancellationToken ct = default)
    {
        var me = RequireCourierId();
        var row = await _db.StaffDeviceTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == me, ct);
        if (row is null) return;
        _db.StaffDeviceTokens.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ExpireDueAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var due = await _db.CourierOffers.IgnoreQueryFilters()
            .Where(o => o.Status == CourierOfferItemStatus.Pending && o.ExpiresAt <= nowUtc)
            .OrderBy(o => o.ExpiresAt)
            .Take(500)
            .ToListAsync(ct);
        if (due.Count == 0) return 0;

        foreach (var o in due)
        {
            o.Status = CourierOfferItemStatus.Expired;
            o.RespondedAt = nowUtc;
        }

        var orderIds = due.Select(o => o.OnlineOrderId).Distinct().ToList();
        var stillPending = await _db.CourierOffers.IgnoreQueryFilters()
            .Where(o => orderIds.Contains(o.OnlineOrderId) && o.Status == CourierOfferItemStatus.Pending && o.ExpiresAt > nowUtc)
            .Select(o => o.OnlineOrderId)
            .Distinct()
            .ToListAsync(ct);
        var orders = await _db.OnlineOrders.IgnoreQueryFilters()
            .Where(o => orderIds.Contains(o.Id) && !stillPending.Contains(o.Id)
                        && o.CourierUserId == null && o.CourierOfferStatus == CourierOfferStatus.Pending)
            .ToListAsync(ct);
        foreach (var order in orders) order.CourierOfferStatus = CourierOfferStatus.NoCourier;

        await _db.SaveChangesAsync(ct);
        foreach (var g in due.GroupBy(o => o.OnlineOrderId))
            await _notifier.CourierOfferClosedAsync(g.Key, g.Select(o => o.CourierUserId).ToList(), ct);
        foreach (var order in orders)
            await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        return due.Count;
    }

    // ---------------- yordamchilar ----------------

    private Guid RequireCourierId() =>
        _currentUser.UserId ?? throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");

    /// <summary>Push type=courier_offer { orderId, offerId } — so'rovni kutdirmaydi (fon Task), xato yutiladi.</summary>
    private async Task PushOffersAsync(OnlineOrder order, IReadOnlyList<CourierOffer> offers, CancellationToken ct)
    {
        if (offers.Count == 0) return;
        var courierIds = offers.Select(o => o.CourierUserId).ToList();
        var tokens = await _db.StaffDeviceTokens.IgnoreQueryFilters().AsNoTracking()
            .Where(t => courierIds.Contains(t.UserId))
            .Select(t => new { t.UserId, t.Token })
            .ToListAsync(ct);
        if (tokens.Count == 0) return;

        var branch = await _db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.Id == order.BranchId).Select(b => new { b.Latitude, b.Longitude }).FirstOrDefaultAsync(ct);
        var distance = branch?.Latitude is { } bla && branch.Longitude is { } blo && order.Latitude is { } la && order.Longitude is { } lo
            ? $" · {ShopCatalogService.HaversineKm(bla, blo, la, lo):0.#} km"
            : string.Empty;
        var body = $"Yangi buyurtma #{order.Number}{distance} · {order.TotalAmount:N0} so'm";
        var byCourier = offers.ToDictionary(o => o.CourierUserId, o => o.Id);

        _ = Task.Run(async () =>
        {
            foreach (var t in tokens)
            {
                try
                {
                    await _push.SendAsync(t.Token, "Yangi buyurtma", body, new Dictionary<string, string>
                    {
                        ["type"] = "courier_offer",
                        ["orderId"] = order.Id.ToString(),
                        ["offerId"] = byCourier[t.UserId].ToString()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kuryerga taklif push'i yuborilmadi");
                }
            }
        }, CancellationToken.None);
    }

    private static string? Cut(string? s, int max) => s is not null && s.Length > max ? s[..max] : s;
}
