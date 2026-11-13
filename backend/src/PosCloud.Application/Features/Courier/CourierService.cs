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
    Task<IReadOnlyList<CourierOrderDto>> GetOrdersAsync(bool history, CancellationToken ct = default);
    Task<CourierOrderDetailDto> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected, CancellationToken ct = default);
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

    public CourierService(IApplicationDbContext db, ICurrentUser currentUser, IOnlineOrderNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
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

    public async Task<IReadOnlyList<CourierOrderDto>> GetOrdersAsync(bool history, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
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
            query = query.Where(o => o.CourierUserId == me.Id && o.Status == OnlineOrderStatus.OnTheWay);
        }

        var orders = await query.OrderByDescending(o => o.DispatchedAt).Take(200).ToListAsync(ct);
        var storeName = await StoreNameAsync(me.OrganizationId, ct);
        return orders.Select(o => Fill(new CourierOrderDto(), o, storeName)).ToList();
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
                ProductName = i.ProductName,
                Quantity = OnlineOrderRules.ReservedQuantity(i),
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            })
            .ToList();
        return dto;
    }

    public async Task<CourierOrderDto> DeliveredAsync(Guid id, decimal? cashCollected, CancellationToken ct = default)
    {
        var me = await RequireCourier(ct);
        var order = await LoadMine(me.Id, id, ct);

        if (order.PaymentStatus == OrderPaymentStatus.Unpaid)
        {
            switch (order.PaymentMethod)
            {
                case PaymentMethod.Cash:
                    order.CourierCashCollected = cashCollected ?? order.TotalAmount;
                    break;
                case PaymentMethod.Card:
                    break; // kuryer terminali
                default:
                    throw new ValidationException("To'lov hali tasdiqlanmagan. Mijoz to'lovini kuting yoki do'konga qo'ng'iroq qiling.");
            }

            order.PaymentStatus = OrderPaymentStatus.Paid;
            order.PaymentConfirmedAt = DateTime.UtcNow;
            order.PaymentConfirmedByUserId = me.Id;
        }

        order.Status = OnlineOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        // Cashback — mijoz qabul qilgandan keyin (shu tranzaksiyada saqlanadi).
        await OnlineOrderCashback.EarnOnDeliveredAsync(_db, order, ct);

        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: true, ct);
        return Fill(new CourierOrderDto(), order, await StoreNameAsync(me.OrganizationId, ct));
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
