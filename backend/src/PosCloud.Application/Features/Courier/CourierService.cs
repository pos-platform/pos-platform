using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Courier.Dtos;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Courier;

public interface ICourierService
{
    Task<CourierMeDto> GetMeAsync(CancellationToken ct = default);
    /// <summary>Faol: yo'ldagi va qabul qilingan (Ready) yetkazishlar + qaytarish olib ketish vazifalari (kind). Tarix: bugungi.</summary>
    Task<IReadOnlyList<object>> GetOrdersAsync(bool history, CancellationToken ct = default);
    Task<CourierOrderDetailDto> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected, CancellationToken ct = default);

    /// <summary>Yetkazish; rejectedItems — eshik oldida rad etilganlar (buyurtma savdosiga qisman qaytarish, ombor keyin).</summary>
    Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected, IReadOnlyList<CourierRejectedItemRequest>? rejectedItems,
        CancellationToken ct = default);

    /// <summary>Qabul qilingan (taklif) buyurtmani do'kondan oldi: Ready → OnTheWay.</summary>
    Task<CourierOrderDto> PickedUpAsync(Guid id, CancellationToken ct = default);
    Task<CourierOrderDto> FailedAsync(Guid id, string reason, CancellationToken ct = default);
}

/// <summary>
/// Kuryer ilovasi (mobil, xodim JWT, Role = Courier). Tenant filtri ishlaydi; kuryer faqat o'ziga biriktirilgan
/// (CourierUserId) buyurtmalarni ko'radi va o'zgartiradi.
/// </summary>
public class CourierService : ICourierService
{
    /// <summary>O'zbekiston vaqti (UTC+5) — "bugun" chegarasi uchun.</summary>
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(5);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IOnlineOrderNotifier _notifier;
    private readonly IShopPushQueue _pushQueue;
    private readonly Sales.ISaleService _sales;

    public CourierService(IApplicationDbContext db, ICurrentUser currentUser, IOnlineOrderNotifier notifier,
        IShopPushQueue pushQueue, Sales.ISaleService sales)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _pushQueue = pushQueue;
        _sales = sales;
    }

    public async Task<CourierMeDto> GetMeAsync(CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        return new CourierMeDto
        {
            Id = me.Id,
            Name = CourierRules.DisplayName(me),
            Phone = me.Phone,
            BranchId = me.BranchId,
            BranchName = me.Branch?.Name,
            StoreName = await StoreNameAsync(me.OrganizationId, ct)
        };
    }

    public async Task<IReadOnlyList<object>> GetOrdersAsync(bool history, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        var orders = await GetDeliveryOrdersAsync(me, history, ct);
        var storeName = await StoreNameAsync(me.OrganizationId, ct);
        var result = new List<object>(orders.Select(o => Fill(new CourierOrderDto(), o, storeName)));
        if (history) return result;

        // Qaytarishni olib ketish vazifalari (kuryerga biriktirilgan, hali olinmagan).
        var returns = await _db.OnlineReturns.AsNoTracking()
            .Include(r => r.Items)
            .Include(r => r.OnlineOrder).ThenInclude(o => o!.Branch)
            .Where(r => r.CourierId == me.Id && r.Status == OnlineReturnStatus.PickupAssigned)
            .OrderBy(r => r.PickupAssignedAt)
            .Take(100)
            .ToListAsync(ct);
        result.AddRange(returns.Select(r => new CourierReturnTaskDto
        {
            Id = r.Id,
            Number = r.Number,
            OrderId = r.OnlineOrderId,
            OrderNumber = r.OnlineOrder?.Number ?? 0,
            Status = r.Status,
            CustomerName = r.OnlineOrder?.CustomerName ?? string.Empty,
            CustomerPhone = r.OnlineOrder?.CustomerPhone ?? string.Empty,
            Address = r.OnlineOrder?.AddressLine,
            Entrance = r.OnlineOrder?.Entrance,
            Floor = r.OnlineOrder?.Floor,
            Apartment = r.OnlineOrder?.Apartment,
            Landmark = r.OnlineOrder?.Landmark,
            Latitude = r.OnlineOrder?.Latitude,
            Longitude = r.OnlineOrder?.Longitude,
            Comment = r.Comment,
            PickupAssignedAt = r.PickupAssignedAt,
            StoreName = storeName,
            BranchAddress = r.OnlineOrder?.Branch?.Address,
            Items = r.Items.OrderBy(i => i.CreatedAt).Select(i => new CourierOrderItemDto
            {
                Id = i.OnlineOrderItemId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = Math.Round(i.UnitPrice * i.Quantity, 2, MidpointRounding.AwayFromZero)
            }).ToList()
        }));
        return result;
    }

    private async Task<List<OnlineOrder>> GetDeliveryOrdersAsync(User me, bool history, CancellationToken ct)
    {
        var query = _db.OnlineOrders.AsNoTracking().Include(o => o.Branch).Include(o => o.Items).AsQueryable();

        if (history)
        {
            var localToday = (DateTime.UtcNow + LocalOffset).Date;
            var since = DateTime.SpecifyKind(localToday - LocalOffset, DateTimeKind.Utc);
            query = query.Where(o =>
                (o.CourierUserId == me.Id && o.Status == OnlineOrderStatus.Delivered && o.DeliveredAt >= since)
                || (o.CourierFailedByUserId == me.Id && o.CourierFailedAt >= since));
        }
        else
        {
            // Yo'ldagilar va taklif bo'yicha qabul qilingan (do'kondan olinishi kutilayotgan) Ready buyurtmalar.
            query = query.Where(o => o.CourierUserId == me.Id
                                     && (o.Status == OnlineOrderStatus.OnTheWay || o.Status == OnlineOrderStatus.Ready));
        }

        return await query.OrderByDescending(o => o.DispatchedAt ?? o.CourierAcceptedAt).Take(200).ToListAsync(ct);
    }

    public async Task<CourierOrderDetailDto> GetOrderAsync(Guid id, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        var order = await _db.OnlineOrders.AsNoTracking()
                        .Include(o => o.Branch).Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == id
                                                  && (o.CourierUserId == me.Id || o.CourierFailedByUserId == me.Id), ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");

        var dto = Fill(new CourierOrderDetailDto(), order, await StoreNameAsync(me.OrganizationId, ct));
        dto.Items = order.Items
            .Where(i => !i.IsRemoved && OnlineOrderRules.ReservedQuantity(i) > 0)
            .OrderBy(i => i.CreatedAt)
            .Select(i => new CourierOrderItemDto
            {
                Id = i.Id,
                RejectedQuantity = i.RejectedQuantity,
                ProductName = i.ProductName,
                Quantity = OnlineOrderRules.ReservedQuantity(i),
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            })
            .ToList();
        return dto;
    }

    public Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected, CancellationToken ct = default) =>
        DeliveredAsync(id, cashCollected, null, ct);

    public async Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected,
        IReadOnlyList<CourierRejectedItemRequest>? rejectedItems, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        var order = await LoadMine(me.Id, id, ct);

        if (order.PaymentStatus == OrderPaymentStatus.Unpaid
            && order.PaymentMethod is not (PaymentMethod.Cash or PaymentMethod.Card))
            throw new ValidationException("To'lov hali tasdiqlanmagan. Mijoz to'lovini kuting yoki do'konga qo'ng'iroq qiling.");

        order.Status = OnlineOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        // Eshik oldida rad etish: buyurtma savdosiga qisman qaytarish (ombor qoldig'i bu yerda emas —
        // tovar do'konga kelganda "rejected-received"da qaytadi). Keshbek keyin faqat qolgan tovarlarga yoziladi.
        if (rejectedItems is { Count: > 0 })
            await RejectAtDoorAsync(order, rejectedItems, ct);

        if (order.PaymentStatus == OrderPaymentStatus.Unpaid)
        {
            if (order.PaymentMethod == PaymentMethod.Cash)
                order.CourierCashCollected = cashCollected ?? order.TotalAmount; // Card — kuryer terminali
            order.PaymentStatus = OrderPaymentStatus.Paid;
            order.PaymentConfirmedAt = DateTime.UtcNow;
            order.PaymentConfirmedByUserId = me.Id;
        }

        // Cashback — mijoz qabul qilgandan keyin (shu tranzaksiyada saqlanadi).
        await OnlineOrderCashback.EarnOnDeliveredAsync(_db, order, ct);
        // Do'stni taklif qilish mukofoti (birinchi yetkazilgan buyurtma, idempotent).
        var referralPushes = await ShopMarketing.ShopReferralRewards.RewardOnDeliveredAsync(_db, order, ct);

        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: true, ct);
        foreach (var push in referralPushes) _pushQueue.Enqueue(push);
        return Fill(new CourierOrderDto(), order, await StoreNameAsync(me.OrganizationId, ct));
    }

    public async Task<CourierOrderDto> PickedUpAsync(Guid id, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        var order = await _db.OnlineOrders.Include(o => o.Branch).Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == id && o.CourierUserId == me.Id, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        if (order.Status == OnlineOrderStatus.OnTheWay)
            return Fill(new CourierOrderDto(), order, await StoreNameAsync(me.OrganizationId, ct)); // takroriy
        if (order.Status != OnlineOrderStatus.Ready)
            throw new ValidationException($"Buyurtma holati \"{OnlineOrderService.StatusText(order.Status)}\" — bu amalni bajarib bo'lmaydi.");

        order.Status = OnlineOrderStatus.OnTheWay;
        order.DispatchedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: true, ct);
        return Fill(new CourierOrderDto(), order, await StoreNameAsync(me.OrganizationId, ct));
    }

    /// <summary>Rad etilgan qatorlarni yozadi, savdoga qisman refund qiladi va buyurtma summasini kamaytiradi.</summary>
    private async Task RejectAtDoorAsync(OnlineOrder order, IReadOnlyList<CourierRejectedItemRequest> rejected,
        CancellationToken ct)
    {
        if (order.SaleId is not { } saleId) throw new ValidationException("Buyurtma savdosi topilmadi.");

        var lines = new List<(Guid ProductId, decimal Quantity)>();
        decimal gross = 0;
        foreach (var g in rejected.GroupBy(r => r.ItemId))
        {
            var item = order.Items.FirstOrDefault(i => i.Id == g.Key && !i.IsRemoved)
                       ?? throw new ValidationException("Rad etilgan qator buyurtmada topilmadi.");
            var qty = g.Sum(x => x.Quantity);
            var left = OnlineOrderRules.ReservedQuantity(item) - item.RejectedQuantity;
            if (qty <= 0 || decimal.Round(qty, 3) != qty || qty > left)
                throw new ValidationException($"\"{item.ProductName}\" uchun rad etilgan miqdor noto'g'ri (ko'pi bilan {left:0.###}).");
            var reason = g.Select(x => x.Reason).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))?.Trim();
            item.RejectedQuantity += qty;
            item.RejectReason = reason is { Length: > 200 } ? reason[..200] : reason;
            lines.Add((item.ProductId, qty));
            gross += Math.Round(item.UnitPrice * qty, 2, MidpointRounding.AwayFromZero);
        }

        var sale = await _db.Sales.AsNoTracking().Include(s => s.Items).FirstAsync(s => s.Id == saleId, ct);
        var request = OnlineReturns.OnlineReturnRules.MapToSaleItems(sale, lines, "Eshik oldida rad etildi");
        var refund = await _sales.RefundAsync(saleId, request, new Sales.Dtos.RefundOptions
        {
            ReturnStock = false,
            AllowWithoutShift = true,
            Note = $"Online #{order.Number}, eshik oldida rad etildi"
        }, ct); // SaveChanges shu ichida (buyurtma o'zgarishlari ham)

        order.ItemsTotal = Math.Max(0, order.ItemsTotal - gross);
        order.TotalAmount = Math.Max(0, order.TotalAmount - refund.TotalAmount);
    }

    public async Task<CourierOrderDto> FailedAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Sababini kiriting.");

        var me = await RequireCourier(ct);
        var order = await LoadMine(me.Id, id, ct);

        // Buyurtma do'konga qaytadi ("Tayyor"), kuryer uziladi, sabab izohga qo'shiladi.
        var note = $"[Kuryer] {reason.Trim()}";
        var comment = string.IsNullOrWhiteSpace(order.Comment) ? note : $"{order.Comment}\n{note}";
        order.Comment = comment.Length > 500 ? comment[^500..] : comment;
        order.Status = OnlineOrderStatus.Ready;
        order.CourierUserId = null;
        order.CourierName = null;
        order.CourierPhone = null;
        order.DispatchedAt = null;
        order.CourierFailedByUserId = me.Id;
        order.CourierFailedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        return Fill(new CourierOrderDto(), order, await StoreNameAsync(me.OrganizationId, ct));
    }

    // ---------------- yordamchilar ----------------

    private async Task<User> RequireCourier(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        var user = await _db.Users.AsNoTracking().Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive || user.Role != UserRole.Courier)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        return user;
    }

    private async Task<OnlineOrder> LoadMine(Guid courierId, Guid id, CancellationToken ct)
    {
        var order = await _db.OnlineOrders.Include(o => o.Branch).Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == id && o.CourierUserId == courierId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        if (order.Status != OnlineOrderStatus.OnTheWay)
            throw new ValidationException($"Buyurtma holati \"{OnlineOrderService.StatusText(order.Status)}\" — bu amalni bajarib bo'lmaydi.");
        return order;
    }

    private async Task<string> StoreNameAsync(Guid? orgId, CancellationToken ct)
    {
        if (orgId is null) return string.Empty;
        var display = await _db.OnlineStoreSettings.AsNoTracking()
            .Where(s => s.OrganizationId == orgId).Select(s => s.DisplayName).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(display)) return display;
        return await _db.Organizations.AsNoTracking()
            .Where(o => o.Id == orgId).Select(o => o.Name).FirstOrDefaultAsync(ct) ?? string.Empty;
    }

    private static T Fill<T>(T dto, OnlineOrder o, string storeName) where T : CourierOrderDto
    {
        dto.Id = o.Id;
        dto.Number = o.Number;
        dto.Status = o.Status;
        dto.CustomerName = o.CustomerName;
        dto.CustomerPhone = o.CustomerPhone;
        dto.AddressLine = o.AddressLine;
        dto.Entrance = o.Entrance;
        dto.Floor = o.Floor;
        dto.Apartment = o.Apartment;
        dto.Intercom = o.Intercom;
        dto.Landmark = o.Landmark;
        dto.Comment = o.Comment;
        dto.Contactless = o.Contactless;
        dto.Latitude = o.Latitude;
        dto.Longitude = o.Longitude;
        dto.TotalAmount = o.TotalAmount;
        dto.PaymentMethod = o.PaymentMethod;
        dto.PaymentStatus = o.PaymentStatus;
        dto.ItemCount = o.Items.Count(i => !i.IsRemoved && OnlineOrderRules.ReservedQuantity(i) > 0);
        dto.DispatchedAt = o.DispatchedAt;
        dto.StoreName = storeName;
        dto.BranchAddress = o.Branch?.Address;
        dto.BranchLatitude = o.Branch?.Latitude;
        dto.BranchLongitude = o.Branch?.Longitude;
        return dto;
    }
}
