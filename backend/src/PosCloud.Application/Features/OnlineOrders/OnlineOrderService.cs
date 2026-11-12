using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.OnlineOrders.Dtos;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.Sales;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineOrders;

public interface IOnlineOrderService
{
    Task<IReadOnlyList<OnlineOrderDto>> GetListAsync(Guid? branchId, bool activeOnly, DateTime? from, CancellationToken ct = default);
    Task<OnlineOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> AcceptAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<OnlineOrderDto> StartPickingAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> UpdatePickingAsync(Guid id, UpdatePickingRequest request, CancellationToken ct = default);
    Task<OnlineOrderDto> ReadyAsync(Guid id, ReadyOnlineOrderRequest request, CancellationToken ct = default);
    Task<OnlineOrderDto> PaymentReceivedAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> DispatchAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> DeliveredAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Xodim (kassa/desktop, admin panel) uchun online buyurtmalar. Oddiy tenant filtri ishlaydi —
/// faqat o'z tashkilotining buyurtmalari ko'rinadi; filialga biriktirilgan xodim faqat o'z filialini ko'radi.
/// "Ready"da mavjud <see cref="ISaleService"/> orqali oddiy savdo yaratiladi (fiskal chek, ombor, hisobotlar).
/// </summary>
public class OnlineOrderService : IOnlineOrderService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISaleService _sales;
    private readonly IOnlineOrderNotifier _notifier;

    public OnlineOrderService(IApplicationDbContext db, ICurrentUser currentUser, ISaleService sales,
        IOnlineOrderNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _sales = sales;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<OnlineOrderDto>> GetListAsync(Guid? branchId, bool activeOnly, DateTime? from,
        CancellationToken ct = default)
    {
        IQueryable<OnlineOrder> query = Scope().AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.Sale)
            .Include(o => o.Items).ThenInclude(i => i.Product);

        var effectiveBranch = _currentUser.BranchId ?? branchId;
        if (effectiveBranch is { } b)
            query = query.Where(o => o.BranchId == b);

        if (activeOnly)
            query = query.Where(o => !OnlineOrderRules.FinishedStatuses.Contains(o.Status));
        else
        {
            var since = DateTime.SpecifyKind((from ?? DateTime.UtcNow.AddDays(-7)).Date, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= since);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(500).ToListAsync(ct);
        return orders.Select(o => OnlineOrderDto.FromEntity(o)).ToList();
    }

    public async Task<OnlineOrderDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct, tracking: false);
        var ids = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var stock = await _db.BranchStocks.AsNoTracking()
            .Where(s => s.BranchId == order.BranchId && ids.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, ct);
        return OnlineOrderDto.FromEntity(order, stock);
    }

    public async Task<OnlineOrderDto> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.New);

        order.Status = OnlineOrderStatus.Accepted;
        order.AcceptedAt = DateTime.UtcNow;
        // Qoldiq yetmasa ham qabul qilinadi — yig'uvchi mijoz tanlagan siyosat bo'yicha hal qiladi.
        await OnlineOrderRules.AdjustReservationAsync(_db, order, +1, ct);

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.New, OnlineOrderStatus.Accepted, OnlineOrderStatus.Picking);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Rad etish sababini kiriting.");

        if (OnlineOrderRules.HoldsReservation(order.Status))
            await OnlineOrderRules.AdjustReservationAsync(_db, order, -1, ct);

        order.Status = OnlineOrderStatus.Rejected;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = reason.Trim();

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> StartPickingAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.Accepted);

        order.Status = OnlineOrderStatus.Picking;
        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> UpdatePickingAsync(Guid id, UpdatePickingRequest request, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.Picking);

        // Rezervni vaqtincha bo'shatib, o'zgarishlardan keyin yangi miqdor bilan qayta qo'yamiz.
        await OnlineOrderRules.AdjustReservationAsync(_db, order, -1, ct);

        foreach (var change in request.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == change.ItemId)
                       ?? throw new NotFoundException("Buyurtma qatori topilmadi.");

            if (change.IsRemoved)
            {
                item.IsRemoved = true;
                item.PickedQuantity = 0;
                continue;
            }

            if (change.PickedQuantity is { } picked)
                ValidateQuantity(picked, item.Product?.IsWeighed ?? false, item.ProductName);

            item.IsRemoved = false;
            item.PickedQuantity = change.PickedQuantity;
        }

        if (request.Add.Count > 0)
        {
            var orgId = order.OrganizationId!.Value;
            var ids = request.Add.Select(a => a.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id) && p.ProductType == ProductType.Good
                            && (p.BranchId == null || p.BranchId == order.BranchId))
                .ToDictionaryAsync(p => p.Id, ct);
            var promotions = await ShopCatalogService.LoadActivePromotionsAsync(_db, orgId, ct);

            foreach (var add in request.Add)
            {
                if (!products.TryGetValue(add.ProductId, out var product))
                    throw new NotFoundException("Mahsulot topilmadi.");
                ValidateQuantity(add.Quantity, product.IsWeighed, product.Name);

                if (add.ReplacesItemId is { } replacedId)
                {
                    var replaced = order.Items.FirstOrDefault(i => i.Id == replacedId)
                                   ?? throw new NotFoundException("Almashtiriladigan qator topilmadi.");
                    replaced.IsRemoved = true;
                    replaced.PickedQuantity = 0;
                }

                var percent = PromotionCalculator.GetDiscountPercent(promotions, product.Id, product.CategoryId);
                var item = new OnlineOrderItem
                {
                    OrganizationId = orgId,
                    OnlineOrderId = order.Id,
                    ProductId = product.Id,
                    Product = product,
                    ProductName = product.Name,
                    UnitPrice = ShopCatalogService.DiscountedPrice(product.Price, percent),
                    Quantity = add.Quantity,
                    PickedQuantity = add.Quantity,
                    ReplacesItemId = add.ReplacesItemId
                };
                order.Items.Add(item);
                _db.OnlineOrderItems.Add(item);
            }
        }

        Recalculate(order);
        await OnlineOrderRules.AdjustReservationAsync(_db, order, +1, ct);

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> ReadyAsync(Guid id, ReadyOnlineOrderRequest request, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.Picking);

        var lines = order.Items.Where(i => !i.IsRemoved && OnlineOrderRules.ReservedQuantity(i) > 0).ToList();
        if (lines.Count == 0)
            throw new ValidationException("Buyurtmada mahsulot qolmadi. Rad eting yoki mahsulot qo'shing.");

        Recalculate(order);

        // Idempotentlik: javob kelmay qayta bosilsa ikkinchi savdo yaratilmaydi (SaleService avvalgisini qaytaradi).
        var clientSaleId = $"online:{order.Id:N}";
        var saleExists = await _db.Sales.AnyAsync(s => s.ClientSaleId == clientSaleId, ct);

        // Rezervni bo'shatamiz — savdo esa ombordan haqiqiy miqdorni ayiradi. Ikkalasi bitta SaveChanges'da saqlanadi.
        // Savdo avvalroq yaratilgan bo'lsa, rezerv o'shanda bo'shatilgan.
        if (!saleExists)
            await OnlineOrderRules.AdjustReservationAsync(_db, order, -1, ct);

        var sale = await _sales.CreateAsync(new CreateSaleRequest
        {
            BranchId = order.BranchId,
            ClientSaleId = clientSaleId,
            TerminalId = request.TerminalId,
            Fiscalize = request.Fiscalize,
            PaymentMethod = order.PaymentMethod,
            TransferSource = order.PaymentMethod == PaymentMethod.Transfer ? "Karta" : null,
            CustomerId = order.CustomerId,
            PaidAmount = Math.Round(order.ItemsTotal, 0, MidpointRounding.AwayFromZero),
            SkipPromotions = true,
            Items = lines.Select(i => new CreateSaleItemRequest
            {
                ProductId = i.ProductId,
                Quantity = OnlineOrderRules.ReservedQuantity(i),
                UnitPriceOverride = i.UnitPrice
            }).ToList()
        }, ct);

        var now = DateTime.UtcNow;
        order.SaleId = sale.Id;
        order.ReadyAt = now;
        order.Status = NextAfterReady(order, await RequireTransferBeforeDispatch(order, ct));

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> PaymentReceivedAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        if (order.Status is OnlineOrderStatus.Rejected or OnlineOrderStatus.Cancelled)
            throw new ValidationException("Bekor qilingan buyurtma.");
        if (order.PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new ValidationException("To'lov allaqachon tasdiqlangan.");

        MarkPaid(order);
        if (order.Status == OnlineOrderStatus.AwaitingTransfer)
            order.Status = order.FulfillmentType == FulfillmentType.Pickup
                ? OnlineOrderStatus.ReadyForPickup
                : OnlineOrderStatus.Ready;

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> DispatchAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        if (order.Status == OnlineOrderStatus.AwaitingTransfer)
            throw new ValidationException("Avval o'tkazma tushganini tasdiqlang.");
        Require(order, OnlineOrderStatus.Ready);
        if (order.FulfillmentType != FulfillmentType.Delivery)
            throw new ValidationException("Olib ketish buyurtmasi kuryerga berilmaydi.");

        order.Status = OnlineOrderStatus.OnTheWay;
        order.DispatchedAt = DateTime.UtcNow;

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> DeliveredAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.OnTheWay, OnlineOrderStatus.ReadyForPickup);

        if (order.PaymentStatus == OrderPaymentStatus.Unpaid)
        {
            if (order.PaymentMethod == PaymentMethod.Transfer)
                throw new ValidationException("O'tkazma hali tasdiqlanmagan. Avval \"To'lov tushdi\"ni bosing.");
            // Naqd yoki terminal — topshirishda olinadi.
            MarkPaid(order);
        }

        order.Status = OnlineOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    // ---------------- yordamchilar ----------------

    private async Task SaveAndNotify(OnlineOrder order, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, ct);
    }

    /// <summary>Tenant filtri ostidagi buyurtmalar; filialga biriktirilgan xodim — faqat o'z filiali.</summary>
    private IQueryable<OnlineOrder> Scope()
    {
        var query = _db.OnlineOrders.AsQueryable();
        if (_currentUser.BranchId is { } branchId)
            query = query.Where(o => o.BranchId == branchId);
        return query;
    }

    private async Task<OnlineOrder> Load(Guid id, CancellationToken ct, bool tracking = true)
    {
        IQueryable<OnlineOrder> query = Scope()
            .Include(o => o.Branch)
            .Include(o => o.Sale)
            .Include(o => o.Items).ThenInclude(i => i.Product);
        if (!tracking) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(o => o.Id == id, ct)
               ?? throw new NotFoundException("Buyurtma topilmadi.");
    }

    private static void Require(OnlineOrder order, params OnlineOrderStatus[] allowed)
    {
        if (!allowed.Contains(order.Status))
            throw new ValidationException($"Buyurtma holati \"{StatusText(order.Status)}\" — bu amalni bajarib bo'lmaydi.");
    }

    private static void ValidateQuantity(decimal qty, bool isWeighed, string name)
    {
        if (qty <= 0 || qty > 10000 || decimal.Round(qty, 3) != qty || (!isWeighed && qty != Math.Floor(qty)))
            throw new ValidationException($"\"{name}\" miqdori noto'g'ri.");
    }

    /// <summary>Yig'ilgan miqdor bo'yicha summa. Yetkazish narxi buyurtmadagi qiymatida qoladi.</summary>
    private static void Recalculate(OnlineOrder order)
    {
        foreach (var i in order.Items)
            i.LineTotal = Math.Round(i.UnitPrice * OnlineOrderRules.ReservedQuantity(i), 2, MidpointRounding.AwayFromZero);

        order.ItemsTotal = order.Items.Sum(i => i.LineTotal);
        order.TotalAmount = order.ItemsTotal + order.DeliveryFee;
    }

    private async Task<bool> RequireTransferBeforeDispatch(OnlineOrder order, CancellationToken ct) =>
        await _db.OnlineStoreSettings
            .Where(s => s.OrganizationId == order.OrganizationId)
            .Select(s => (bool?)s.RequireTransferBeforeDispatch)
            .FirstOrDefaultAsync(ct) ?? true;

    private static OnlineOrderStatus NextAfterReady(OnlineOrder order, bool requireTransferFirst)
    {
        if (order.PaymentMethod == PaymentMethod.Transfer
            && order.PaymentStatus == OrderPaymentStatus.Unpaid
            && (requireTransferFirst || order.FulfillmentType == FulfillmentType.Pickup))
            return OnlineOrderStatus.AwaitingTransfer;

        return order.FulfillmentType == FulfillmentType.Pickup
            ? OnlineOrderStatus.ReadyForPickup
            : OnlineOrderStatus.Ready;
    }

    private void MarkPaid(OnlineOrder order)
    {
        order.PaymentStatus = OrderPaymentStatus.Paid;
        order.PaymentConfirmedAt = DateTime.UtcNow;
        order.PaymentConfirmedByUserId = _currentUser.UserId;
    }

    public static string StatusText(OnlineOrderStatus s) => s switch
    {
        OnlineOrderStatus.New => "Yangi",
        OnlineOrderStatus.Accepted => "Qabul qilindi",
        OnlineOrderStatus.Picking => "Yig'ilmoqda",
        OnlineOrderStatus.Ready => "Tayyor",
        OnlineOrderStatus.AwaitingTransfer => "O'tkazma kutilmoqda",
        OnlineOrderStatus.OnTheWay => "Yo'lda",
        OnlineOrderStatus.ReadyForPickup => "Olib ketishga tayyor",
        OnlineOrderStatus.Delivered => "Yetkazildi",
        OnlineOrderStatus.Rejected => "Rad etildi",
        OnlineOrderStatus.Cancelled => "Bekor qilindi",
        _ => s.ToString()
    };
}
