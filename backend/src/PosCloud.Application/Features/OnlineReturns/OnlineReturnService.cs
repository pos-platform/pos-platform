using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Courier;
using PosCloud.Application.Features.OnlineReturns.Dtos;
using PosCloud.Application.Features.Sales;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineReturns;

public interface IOnlineReturnService
{
    // ---- Mijoz (tenant filtri yo'q — ShopUserId qo'lda) ----
    Task EnsureCanCreateAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default);
    Task<OnlineReturnDto> CreateAsync(Guid shopUserId, Guid orderId, CreateReturnRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<OnlineReturnDto>> GetMineAsync(Guid shopUserId, CancellationToken ct = default);
    Task<OnlineReturnDto> GetMineAsync(Guid shopUserId, Guid id, CancellationToken ct = default);
    Task<OnlineReturnDto> CancelMineAsync(Guid shopUserId, Guid id, CancellationToken ct = default);

    // ---- Xodim ----
    Task<IReadOnlyList<OnlineReturnDto>> GetListAsync(OnlineReturnStatus? status, Guid? branchId, CancellationToken ct = default);
    Task<OnlineReturnDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<OnlineReturnDto> ApproveAsync(Guid id, OnlineReturnMethod method, CancellationToken ct = default);
    Task<OnlineReturnDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<OnlineReturnDto> AssignCourierAsync(Guid id, Guid courierId, CancellationToken ct = default);
    Task<OnlineReturnDto> ReceivedAsync(Guid id, CancellationToken ct = default);
    Task<OnlineReturnDto> RefundedAsync(Guid id, CancellationToken ct = default);

    // ---- Kuryer ----
    Task<OnlineReturnDto> CourierPickedUpAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Yetkazilgandan keyingi qaytarish (10.5): mijoz so'rov yuboradi → do'kon tasdiqlaydi/rad etadi → kuryer olib ketadi
/// yoki mijoz olib keladi → "Do'konga keldi"da qisman refund (keshbek ayiriladi, Bonus usulida pul o'rniga ball)
/// → "Pul berildi". Kuryer qaror qabul qilmaydi va naqd bermaydi.
/// </summary>
public class OnlineReturnService : IOnlineReturnService
{
    public const int MaxPhotos = 3;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISaleService _sales;
    private readonly IOnlineOrderNotifier _notifier;

    public OnlineReturnService(IApplicationDbContext db, ICurrentUser currentUser, ISaleService sales,
        IOnlineOrderNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _sales = sales;
        _notifier = notifier;
    }

    // ================= Mijoz =================

    public async Task EnsureCanCreateAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadShopOrderAsync(shopUserId, orderId, ct);
        var settings = await SettingsAsync(order.OrganizationId, ct);
        EnsureReturnable(order, settings);
    }

    public async Task<OnlineReturnDto> CreateAsync(Guid shopUserId, Guid orderId, CreateReturnRequest request,
        CancellationToken ct = default)
    {
        var order = await LoadShopOrderAsync(shopUserId, orderId, ct, tracking: true);
        var settings = await SettingsAsync(order.OrganizationId, ct);
        EnsureReturnable(order, settings);

        if (!Enum.IsDefined(request.Reason)) throw new ValidationException("Qaytarish sababini tanlang.");
        if (!Enum.IsDefined(request.RefundMethod)) throw new ValidationException("Pul qaytarish usulini tanlang.");
        if (request.RefundMethod == ReturnRefundMethod.Bonus && order.CustomerId is null)
            throw new ValidationException("Bonus bilan qaytarish uchun do'kon mijoz kartasi kerak.");
        if (request.PhotoUrls.Count > MaxPhotos) throw new ValidationException($"Ko'pi bilan {MaxPhotos} ta rasm.");
        if (request.Items.Count == 0) throw new ValidationException("Qaytariladigan tovarni tanlang.");

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        if (comment is { Length: > 500 }) comment = comment[..500];

        var ret = new OnlineReturn
        {
            OrganizationId = order.OrganizationId,
            OnlineOrderId = order.Id,
            BranchId = order.BranchId,
            ShopUserId = shopUserId,
            Status = OnlineReturnStatus.Requested,
            Method = OnlineReturnMethod.BringToStore,
            Reason = request.Reason,
            Comment = comment,
            PhotoUrls = request.PhotoUrls.ToList(),
            RefundMethod = request.RefundMethod
        };

        foreach (var group in request.Items.GroupBy(i => i.ItemId))
        {
            var item = order.Items.FirstOrDefault(i => i.Id == group.Key && !i.IsRemoved)
                       ?? throw new ValidationException("Buyurtma qatori topilmadi.");
            var qty = group.Sum(x => x.Quantity);
            if (qty <= 0 || decimal.Round(qty, 3) != qty)
                throw new ValidationException($"\"{item.ProductName}\" miqdori noto'g'ri.");
            if (item.Product is { IsWeighed: false } && qty != Math.Floor(qty))
                throw new ValidationException($"\"{item.ProductName}\" miqdori butun son bo'lishi kerak.");
            var left = OnlineReturnRules.Returnable(item);
            if (qty > left)
                throw new ValidationException($"\"{item.ProductName}\" dan ko'pi bilan {left:0.###} qaytarish mumkin.");

            item.ReturnedQuantity += qty; // band qilinadi (bekor/rad etilsa qaytadi)
            ret.Items.Add(new OnlineReturnItem
            {
                OrganizationId = order.OrganizationId,
                OnlineOrderItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = qty,
                UnitPrice = item.UnitPrice
            });
        }

        _db.OnlineReturns.Add(ret);
        await SaveWithNumberAsync(ret, ct);
        await _notifier.ReturnChangedAsync(ret, isNew: true, pushToCustomer: false, ct);
        return await GetMineAsync(shopUserId, ret.Id, ct);
    }

    public async Task<IReadOnlyList<OnlineReturnDto>> GetMineAsync(Guid shopUserId, CancellationToken ct = default)
    {
        var returns = await _db.OnlineReturns.IgnoreQueryFilters().AsNoTracking()
            .Include(r => r.Items)
            .Include(r => r.OnlineOrder).ThenInclude(o => o!.Items).ThenInclude(i => i.Product)
            .Where(r => r.ShopUserId == shopUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        var names = await StoreNamesAsync(returns.Select(r => r.OrganizationId).ToList(), ct);
        return returns.Select(r => OnlineReturnDto.FromEntity(r, r.OnlineOrder, null, includeCustomer: false,
            courierPickupAllowed: false, storeName: names.GetValueOrDefault(r.OrganizationId ?? Guid.Empty))).ToList();
    }

    public async Task<OnlineReturnDto> GetMineAsync(Guid shopUserId, Guid id, CancellationToken ct = default)
    {
        var r = await _db.OnlineReturns.IgnoreQueryFilters().AsNoTracking()
                    .Include(x => x.Items)
                    .Include(x => x.OnlineOrder).ThenInclude(o => o!.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(x => x.Id == id && x.ShopUserId == shopUserId, ct)
                ?? throw new NotFoundException("Qaytarish topilmadi.");
        var branch = await _db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.Id == r.BranchId).Select(b => b.Name).FirstOrDefaultAsync(ct);
        var names = await StoreNamesAsync([r.OrganizationId], ct);
        return OnlineReturnDto.FromEntity(r, r.OnlineOrder, branch, includeCustomer: false, courierPickupAllowed: false,
            storeName: names.GetValueOrDefault(r.OrganizationId ?? Guid.Empty));
    }

    public async Task<OnlineReturnDto> CancelMineAsync(Guid shopUserId, Guid id, CancellationToken ct = default)
    {
        var r = await _db.OnlineReturns.IgnoreQueryFilters()
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == id && x.ShopUserId == shopUserId, ct)
                ?? throw new NotFoundException("Qaytarish topilmadi.");
        if (r.Status != OnlineReturnStatus.Requested)
            throw new ValidationException("Faqat ko'rib chiqilmagan so'rovni bekor qilish mumkin.");

        await ReleaseAsync(r, ct);
        r.Status = OnlineReturnStatus.Cancelled;
        r.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _notifier.ReturnChangedAsync(r, isNew: false, pushToCustomer: false, ct);
        return await GetMineAsync(shopUserId, id, ct);
    }

    // ================= Xodim =================

    public async Task<IReadOnlyList<OnlineReturnDto>> GetListAsync(OnlineReturnStatus? status, Guid? branchId,
        CancellationToken ct = default)
    {
        var query = Scope().AsNoTracking()
            .Include(r => r.Items)
            .Include(r => r.OnlineOrder).ThenInclude(o => o!.Items).ThenInclude(i => i.Product)
            .AsQueryable();
        if (_currentUser.BranchId is null && branchId is { } b) query = query.Where(r => r.BranchId == b);
        if (status is { } s) query = query.Where(r => r.Status == s);

        var list = await query.OrderByDescending(r => r.CreatedAt).Take(500).ToListAsync(ct);
        var branches = await BranchNamesAsync(list.Select(r => r.BranchId).Distinct().ToList(), ct);
        var pickup = await CourierPickupAllowedAsync(ct);
        var receipts = await ReceiptNumbersAsync(list.Where(r => r.RefundSaleId != null).Select(r => r.RefundSaleId!.Value).ToList(), ct);
        return list.Select(r => OnlineReturnDto.FromEntity(r, r.OnlineOrder, branches.GetValueOrDefault(r.BranchId), true, pickup,
            r.RefundSaleId is { } sid ? receipts.GetValueOrDefault(sid) : null)).ToList();
    }

    public async Task<OnlineReturnDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var r = await Scope().AsNoTracking()
                    .Include(x => x.Items)
                    .Include(x => x.OnlineOrder).ThenInclude(o => o!.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Qaytarish topilmadi.");
        var branches = await BranchNamesAsync([r.BranchId], ct);
        var receipts = r.RefundSaleId is { } sid ? await ReceiptNumbersAsync([sid], ct) : new Dictionary<Guid, string>();
        return OnlineReturnDto.FromEntity(r, r.OnlineOrder, branches.GetValueOrDefault(r.BranchId), true,
            await CourierPickupAllowedAsync(ct), r.RefundSaleId is { } s ? receipts.GetValueOrDefault(s) : null);
    }

    public async Task<OnlineReturnDto> ApproveAsync(Guid id, OnlineReturnMethod method, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        Require(r, OnlineReturnStatus.Requested);
        if (!Enum.IsDefined(method)) throw new ValidationException("Qaytarish usulini tanlang.");
        if (method == OnlineReturnMethod.CourierPickup && !await CourierPickupAllowedAsync(ct))
            throw new ValidationException("Kuryer orqali olib ketish sozlamada o'chirilgan.");

        r.Status = OnlineReturnStatus.Approved;
        r.Method = method;
        r.ApprovedAt = DateTime.UtcNow;
        return await SaveAndNotifyAsync(r, ct);
    }

    public async Task<OnlineReturnDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        Require(r, OnlineReturnStatus.Requested, OnlineReturnStatus.Approved, OnlineReturnStatus.PickupAssigned);
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Rad etish sababini kiriting.");

        await ReleaseAsync(r, ct);
        r.Status = OnlineReturnStatus.Rejected;
        r.RejectReason = reason.Trim().Length > 500 ? reason.Trim()[..500] : reason.Trim();
        r.RejectedAt = DateTime.UtcNow;
        return await SaveAndNotifyAsync(r, ct);
    }

    public async Task<OnlineReturnDto> AssignCourierAsync(Guid id, Guid courierId, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        Require(r, OnlineReturnStatus.Approved, OnlineReturnStatus.PickupAssigned);
        if (r.Method != OnlineReturnMethod.CourierPickup)
            throw new ValidationException("Bu qaytarishni mijoz do'konga olib keladi.");

        var courier = await _db.Users.AsNoTracking()
                          .FirstOrDefaultAsync(u => u.Id == courierId && u.Role == UserRole.Courier && u.IsActive, ct)
                      ?? throw new ValidationException("Kuryer topilmadi yoki faol emas.");
        if (courier.BranchId is { } cb && cb != r.BranchId)
            throw new ValidationException("Kuryer boshqa filialga biriktirilgan.");

        r.CourierId = courier.Id;
        r.CourierName = CourierRules.DisplayName(courier) is { Length: > 100 } n ? n[..100] : CourierRules.DisplayName(courier);
        r.CourierPhone = courier.Phone is { Length: > 20 } p ? p[..20] : courier.Phone;
        r.Status = OnlineReturnStatus.PickupAssigned;
        r.PickupAssignedAt = DateTime.UtcNow;
        return await SaveAndNotifyAsync(r, ct);
    }

    public async Task<OnlineReturnDto> ReceivedAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        if (r.Method == OnlineReturnMethod.CourierPickup)
            Require(r, OnlineReturnStatus.PickedUp, OnlineReturnStatus.PickupAssigned);
        else
            Require(r, OnlineReturnStatus.Approved);

        var order = await _db.OnlineOrders.Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == r.OnlineOrderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        if (order.SaleId is not { } saleId) throw new ValidationException("Buyurtma savdosi topilmadi.");
        var sale = await _db.Sales.AsNoTracking().Include(s => s.Items).FirstAsync(s => s.Id == saleId, ct);

        // Qisman refund: keshbek mutanosib ayiriladi, balans yetmasa — puldan ushlab qolinadi; ombor qoldig'i qaytadi.
        var request = OnlineReturnRules.MapToSaleItems(sale, r.Items.Select(i => (i.ProductId, i.Quantity)),
            $"Online qaytarish #{r.Number}");
        var options = new RefundOptions
        {
            Note = $"Online #{order.Number}, qaytarish #{r.Number}",
            CashAsBonus = r.RefundMethod == ReturnRefundMethod.Bonus,
            AllowWithoutShift = r.RefundMethod == ReturnRefundMethod.Bonus,
            PaymentMethod = r.RefundMethod switch
            {
                ReturnRefundMethod.Cash => PaymentMethod.Cash,
                ReturnRefundMethod.Transfer => PaymentMethod.Transfer,
                _ => null
            }
        };

        var now = DateTime.UtcNow;
        r.Status = OnlineReturnStatus.Received;
        r.ReceivedAt = now;
        r.PickedUpAt ??= r.Method == OnlineReturnMethod.CourierPickup ? now : null;

        var refund = await _sales.RefundAsync(saleId, request, options, ct); // SaveChanges shu ichida
        r.RefundSaleId = refund.Id;
        r.RefundAmount = refund.TotalAmount;
        r.LoyaltyWithheldAmount = refund.LoyaltyWithheldAmount;
        if (r.RefundMethod == ReturnRefundMethod.Bonus)
        {
            // Pul o'rniga ball darhol yozildi — "Pul berildi" bosqichi shart emas.
            r.Status = OnlineReturnStatus.Refunded;
            r.RefundedAt = now;
        }
        return await SaveAndNotifyAsync(r, ct);
    }

    public async Task<OnlineReturnDto> RefundedAsync(Guid id, CancellationToken ct = default)
    {
        var r = await LoadAsync(id, ct);
        Require(r, OnlineReturnStatus.Received);
        r.Status = OnlineReturnStatus.Refunded;
        r.RefundedAt = DateTime.UtcNow;
        return await SaveAndNotifyAsync(r, ct);
    }

    // ================= Kuryer =================

    public async Task<OnlineReturnDto> CourierPickedUpAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        var r = await _db.OnlineReturns.Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CourierId == userId, ct)
                ?? throw new NotFoundException("Qaytarish topilmadi.");
        Require(r, OnlineReturnStatus.PickupAssigned);
        r.Status = OnlineReturnStatus.PickedUp;
        r.PickedUpAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _notifier.ReturnChangedAsync(r, isNew: false, pushToCustomer: true, ct);
        var order = await _db.OnlineOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == r.OnlineOrderId, ct);
        return OnlineReturnDto.FromEntity(r, order, null, includeCustomer: true, courierPickupAllowed: true);
    }

    // ================= yordamchilar =================

    private IQueryable<OnlineReturn> Scope()
    {
        var q = _db.OnlineReturns.AsQueryable();
        if (_currentUser.BranchId is { } b) q = q.Where(r => r.BranchId == b);
        return q;
    }

    private async Task<OnlineReturn> LoadAsync(Guid id, CancellationToken ct) =>
        await Scope().Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new NotFoundException("Qaytarish topilmadi.");

    private async Task<OnlineReturnDto> SaveAndNotifyAsync(OnlineReturn r, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
        await _notifier.ReturnChangedAsync(r, isNew: false, pushToCustomer: true, ct);
        return await GetAsync(r.Id, ct);
    }

    private static void Require(OnlineReturn r, params OnlineReturnStatus[] allowed)
    {
        if (!allowed.Contains(r.Status))
            throw new ValidationException($"Qaytarish holati \"{r.Status}\" — bu amalni bajarib bo'lmaydi.");
    }

    /// <summary>Bekor/rad etilganda buyurtma qatorlaridagi band qilingan miqdor qaytariladi.</summary>
    private async Task ReleaseAsync(OnlineReturn r, CancellationToken ct)
    {
        var ids = r.Items.Select(i => i.OnlineOrderItemId).ToList();
        var items = await _db.OnlineOrderItems.IgnoreQueryFilters().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        foreach (var ri in r.Items)
        {
            var item = items.FirstOrDefault(i => i.Id == ri.OnlineOrderItemId);
            if (item is not null) item.ReturnedQuantity = Math.Max(0, item.ReturnedQuantity - ri.Quantity);
        }
    }

    private async Task<OnlineOrder> LoadShopOrderAsync(Guid shopUserId, Guid orderId, CancellationToken ct, bool tracking = false)
    {
        IQueryable<OnlineOrder> q = _db.OnlineOrders.IgnoreQueryFilters()
            .Include(o => o.Items).ThenInclude(i => i.Product);
        if (!tracking) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(o => o.Id == orderId && o.ShopUserId == shopUserId, ct)
               ?? throw new NotFoundException("Buyurtma topilmadi.");
    }

    private Task<OnlineStoreSettings?> SettingsAsync(Guid? orgId, CancellationToken ct) =>
        _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

    private static void EnsureReturnable(OnlineOrder order, OnlineStoreSettings? settings)
    {
        if (settings is { ReturnsEnabled: false })
            throw new ValidationException("Bu do'konda qaytarish so'rovi qabul qilinmaydi.");
        if (order.Status != OnlineOrderStatus.Delivered)
            throw new ValidationException("Faqat yetkazilgan buyurtmani qaytarish mumkin.");
        if (!OnlineReturnRules.CanReturn(order, settings, DateTime.UtcNow))
            throw new ValidationException(order.Items.Any(i => OnlineReturnRules.Returnable(i) > 0)
                ? $"Qaytarish muddati ({settings?.ReturnDays ?? 3} kun) o'tgan."
                : "Qaytariladigan tovar qolmagan.");
    }

    private async Task<bool> CourierPickupAllowedAsync(CancellationToken ct)
    {
        var orgId = _currentUser.OrganizationId;
        return await _db.OnlineStoreSettings.AsNoTracking()
            .Where(s => s.OrganizationId == orgId)
            .Select(s => (bool?)s.ReturnPickupByCourier)
            .FirstOrDefaultAsync(ct) ?? true;
    }

    private async Task SaveWithNumberAsync(OnlineReturn ret, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var last = await _db.OnlineReturns.IgnoreQueryFilters()
                .Where(r => r.OrganizationId == ret.OrganizationId)
                .MaxAsync(r => (int?)r.Number, ct) ?? 0;
            ret.Number = last + 1;
            try
            {
                await _db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException) when (attempt < 5)
            {
                // raqam band bo'ldi — keyingisini olamiz
            }
        }
    }

    private async Task<Dictionary<Guid, string>> BranchNamesAsync(List<Guid> ids, CancellationToken ct) =>
        await _db.Branches.IgnoreQueryFilters().AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, ct);

    private async Task<Dictionary<Guid, string>> ReceiptNumbersAsync(List<Guid> saleIds, CancellationToken ct) =>
        saleIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Sales.AsNoTracking().Where(s => saleIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.ReceiptNumber, ct);

    private async Task<Dictionary<Guid, string>> StoreNamesAsync(List<Guid?> orgIds, CancellationToken ct)
    {
        var ids = orgIds.Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var rows = await _db.Organizations.AsNoTracking()
            .Where(o => ids.Contains(o.Id))
            .Select(o => new
            {
                o.Id,
                o.Name,
                Display = _db.OnlineStoreSettings.IgnoreQueryFilters()
                    .Where(s => s.OrganizationId == o.Id).Select(s => s.DisplayName).FirstOrDefault()
            })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => string.IsNullOrWhiteSpace(r.Display) ? r.Name : r.Display!);
    }
}
