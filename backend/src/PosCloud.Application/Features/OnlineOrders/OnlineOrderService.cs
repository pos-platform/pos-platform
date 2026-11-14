using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Loyalty;
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
    Task<IReadOnlyList<OnlineOrderDto>> GetListAsync(Guid? branchId, bool activeOnly, DateTime? from,
        DateTime? to = null, OnlineOrderStatus? status = null, CancellationToken ct = default);
    Task<OnlineOrderDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> AcceptAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<OnlineOrderDto> StartPickingAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> UpdatePickingAsync(Guid id, UpdatePickingRequest request, CancellationToken ct = default);
    Task<OnlineOrderDto> ReadyAsync(Guid id, ReadyOnlineOrderRequest request, CancellationToken ct = default);
    Task<OnlineOrderDto> PaymentReceivedAsync(Guid id, CancellationToken ct = default);
    Task<OnlineOrderDto> DispatchAsync(Guid id, DispatchOnlineOrderRequest? request = null, CancellationToken ct = default);
    Task<OnlineOrderDto> DeliveredAsync(Guid id, CancellationToken ct = default);

    /// <summary>Do'kon tomonidan bekor qilish (Ready, AwaitingTransfer, ReadyForPickup, OnTheWay): savdo qaytariladi.</summary>
    Task<OnlineOrderDto> CancelAsync(Guid id, string reason, CancellationToken ct = default);

    /// <summary>Faol kuryerlar (filial bo'yicha) va ulardagi yo'ldagi buyurtmalar soni.</summary>
    Task<IReadOnlyList<CourierListItemDto>> GetCouriersAsync(Guid? branchId, CancellationToken ct = default);

    /// <summary>Kuryer naqdni topshirdi: kassa kirimi ("Online #N — kuryer topshirdi").</summary>
    Task<OnlineOrderDto> CashHandoverAsync(Guid id, CancellationToken ct = default);

    /// <summary>Kuryerda turgan (topshirilmagan) naqdlar.</summary>
    Task<IReadOnlyList<CourierCashItemDto>> GetCourierCashAsync(Guid? branchId, CancellationToken ct = default);

    /// <summary>Mijoz baholari: o'rtacha, taqsimot, teglar, izohlar (ratedAt bo'yicha; standart — oxirgi 30 kun).</summary>
    Task<OnlineOrderRatingsDto> GetRatingsAsync(DateTime? from, DateTime? to, Guid? branchId, CancellationToken ct = default);

    // ---- Bosqich 10 ----

    /// <summary>Kuryerlarga taklifni (qayta) yuborish.</summary>
    Task<OnlineOrderDto> OfferCouriersAsync(Guid id, CancellationToken ct = default);

    /// <summary>Eshik oldida rad etilgan, hali do'konga qabul qilinmagan buyurtmalar.</summary>
    Task<IReadOnlyList<OnlineOrderDto>> GetRejectedPendingAsync(Guid? branchId, CancellationToken ct = default);

    /// <summary>Rad etilgan tovarlar do'konga keldi: ombor qoldig'i qaytadi, RejectedReturnedAt.</summary>
    Task<OnlineOrderDto> RejectedReceivedAsync(Guid id, CancellationToken ct = default);
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
    private readonly IShopPushQueue _pushQueue;
    private readonly Courier.ICourierOfferService _offers;

    public OnlineOrderService(IApplicationDbContext db, ICurrentUser currentUser, ISaleService sales,
        IOnlineOrderNotifier notifier, IShopPushQueue pushQueue, Courier.ICourierOfferService offers)
    {
        _db = db;
        _currentUser = currentUser;
        _sales = sales;
        _notifier = notifier;
        _pushQueue = pushQueue;
        _offers = offers;
    }

    public async Task<IReadOnlyList<OnlineOrderDto>> GetListAsync(Guid? branchId, bool activeOnly, DateTime? from,
        DateTime? to = null, OnlineOrderStatus? status = null, CancellationToken ct = default)
    {
        IQueryable<OnlineOrder> query = Scope().AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.Sale)
            .Include(o => o.Items).ThenInclude(i => i.Product);

        var effectiveBranch = _currentUser.BranchId ?? branchId;
        if (effectiveBranch is { } b)
            query = query.Where(o => o.BranchId == b);

        if (status is { } st)
            query = query.Where(o => o.Status == st);

        // Holat berilsa — faol filtri o'rniga sana oralig'i ishlaydi (masalan, tarixdagi bekor qilinganlar).
        if (activeOnly && status is null)
            query = query.Where(o => !OnlineOrderRules.FinishedStatuses.Contains(o.Status));
        else
        {
            var since = DateTime.SpecifyKind((from ?? DateTime.UtcNow.AddDays(-7)).Date, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= since);
        }

        if (to is { } toDate)
        {
            // "to" kun bo'yicha, shu kun ham kiradi.
            var until = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < until);
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
        var dto = OnlineOrderDto.FromEntity(order, stock);
        dto.CourierOffers = (await _offers.GetHistoryAsync(order.Id, ct)).ToList();
        return dto;
    }

    // ---------------- Bosqich 10 ----------------

    public async Task<OnlineOrderDto> OfferCouriersAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        await _offers.SendAsync(order, manual: true, ct);
        return await GetAsync(id, ct);
    }

    public async Task<IReadOnlyList<OnlineOrderDto>> GetRejectedPendingAsync(Guid? branchId, CancellationToken ct = default)
    {
        var query = Scope().AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.Sale)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.RejectedReturnedAt == null && o.Items.Any(i => i.RejectedQuantity > 0));
        if (_currentUser.BranchId is null && branchId is { } b)
            query = query.Where(o => o.BranchId == b);

        var orders = await query.OrderBy(o => o.DeliveredAt).Take(200).ToListAsync(ct);
        return orders.Select(o => OnlineOrderDto.FromEntity(o)).ToList();
    }

    public async Task<OnlineOrderDto> RejectedReceivedAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        var lines = order.Items.Where(i => i.RejectedQuantity > 0).Select(i => (i.ProductId, i.RejectedQuantity)).ToList();
        if (lines.Count == 0) throw new ValidationException("Bu buyurtmada rad etilgan tovar yo'q.");
        if (order.RejectedReturnedAt is not null) throw new ValidationException("Rad etilgan tovarlar allaqachon qabul qilingan.");

        // Eshik oldidagi qisman refund ombor qoldig'ini qaytarmagan — tovar shu yerda (do'konga kelganda) qaytadi.
        await _sales.ReturnToStockAsync(order.BranchId, lines, ct);
        order.RejectedReturnedAt = DateTime.UtcNow;
        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
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
        await OnlineOrderPricing.ReleasePromoAsync(_db, order, ct);

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> StartPickingAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        Require(order, OnlineOrderStatus.Accepted);

        order.Status = OnlineOrderStatus.Picking;
        order.PickingStartedAt = DateTime.UtcNow;
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

            if (change.MarkingCodes is not null)
                item.MarkingCodes = NormalizeMarkingCodes(item, change.MarkingCodes);
        }

        // Bir xil kod buyurtmaning ikki qatorida bo'lmasin.
        var allCodes = order.Items.Where(i => !i.IsRemoved).SelectMany(i => i.MarkingCodes).ToList();
        if (allCodes.Count != allCodes.Distinct(StringComparer.Ordinal).Count())
            throw new ValidationException("Bir markirovka kodi ikki marta skanerlangan.");

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

        await RecalculateAsync(order, ct);
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

        // Idempotentlik: javob kelmay qayta bosilsa ikkinchi savdo yaratilmaydi (SaleService avvalgisini qaytaradi).
        var clientSaleId = $"online:{order.Id:N}";
        var saleExists = await _db.Sales.AnyAsync(s => s.ClientSaleId == clientSaleId, ct);

        var settings = await _db.OnlineStoreSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == order.OrganizationId, ct);

        // Yetkazish narxi fiskal chekda: MXIK kiritilgan va narx > 0 bo'lsa — yashirin "Yetkazib berish xizmati" mahsuloti.
        // Mahsulot SaleService uni bazadan o'qishi uchun oldindan saqlanadi (buyurtma hali o'zgartirilmagan).
        Product? deliveryProduct = null;
        var deliveryMxik = settings?.DeliveryServiceMxikCode?.Trim();
        if (!saleExists && order.FulfillmentType == FulfillmentType.Delivery && order.DeliveryFee > 0
            && !string.IsNullOrEmpty(deliveryMxik))
            deliveryProduct = await EnsureDeliveryProductAsync(order.OrganizationId!.Value, deliveryMxik, ct);

        await RecalculateAsync(order, ct);

        // Markirovka: har markirovkali qatorda kodlar soni = butun miqdor.
        foreach (var i in lines.Where(i => i.Product?.IsMarked == true))
        {
            var need = (int)Math.Ceiling(OnlineOrderRules.ReservedQuantity(i));
            if (i.MarkingCodes.Count != need)
                throw new ValidationException(
                    $"\"{i.ProductName}\" markirovkali tovar — {need} ta DataMatrix kod kerak, skanerlangan: {i.MarkingCodes.Count}.");
        }

        Customer? customer = null;
        if (!saleExists)
        {
            // Bonus: "Tayyor" paytidagi ball bilan qayta tekshiramiz — kam bo'lsa borini ishlatamiz va summani qayta hisoblaymiz.
            if (order.BonusUsed > 0)
            {
                customer = order.CustomerId is { } cid
                    ? await _db.Customers.FirstOrDefaultAsync(c => c.Id == cid, ct)
                    : null;
                var available = Math.Max(0, Math.Floor(customer?.LoyaltyPoints ?? 0));
                if (order.BonusUsed > available)
                {
                    order.BonusUsed = available;
                    order.TotalAmount = order.ItemsTotal - order.PromoDiscount - order.BonusUsed + order.DeliveryFee;
                }
            }

            // Rezervni bo'shatamiz — savdo esa ombordan haqiqiy miqdorni ayiradi. Ikkalasi bitta SaveChanges'da saqlanadi.
            // Savdo avvalroq yaratilgan bo'lsa, rezerv o'shanda bo'shatilgan.
            await OnlineOrderRules.AdjustReservationAsync(_db, order, -1, ct);
        }

        // Savdo qatorlari: markirovkali tovar — har kod alohida qator (miqdor 1).
        var saleItems = new List<CreateSaleItemRequest>();
        foreach (var i in lines)
        {
            if (i.Product?.IsMarked == true)
                saleItems.AddRange(i.MarkingCodes.Select(code => new CreateSaleItemRequest
                {
                    ProductId = i.ProductId,
                    Quantity = 1,
                    UnitPriceOverride = i.UnitPrice,
                    MarkingCode = code
                }));
            else
                saleItems.Add(new CreateSaleItemRequest
                {
                    ProductId = i.ProductId,
                    Quantity = OnlineOrderRules.ReservedQuantity(i),
                    UnitPriceOverride = i.UnitPrice
                });
        }

        // Promokod + bonus chegirmasi qatorlarga mutanosib taqsimlanadi (fiskal chek summasi to'g'ri bo'lishi uchun).
        var grosses = saleItems.Select(l => Round2(l.UnitPriceOverride!.Value * l.Quantity)).ToList();
        var discounts = OnlineOrderPricing.Distribute(grosses, order.PromoDiscount + order.BonusUsed);
        for (var k = 0; k < saleItems.Count; k++)
            saleItems[k].DiscountAmount = discounts[k];
        var expectedTotal = grosses.Sum() - discounts.Sum();

        if (deliveryProduct is not null)
        {
            saleItems.Add(new CreateSaleItemRequest
            {
                ProductId = deliveryProduct.Id,
                Quantity = 1,
                UnitPriceOverride = order.DeliveryFee
            });
            expectedTotal += Round2(order.DeliveryFee);
        }

        if (!saleExists)
        {
            if (order.BonusUsed > 0 && customer is not null)
            {
                // Ball yuqorida joriy balans bilan qisqartirilgan — Apply to'liq yozadi (Redeem, Online).
                LoyaltyRules.Apply(_db, customer, -order.BonusUsed, LoyaltyTransactionType.Redeem, LoyaltySource.Online,
                    order.BonusUsed, $"Online #{order.Number} — bonus bilan to'lov");
                order.BonusCharged = true;
            }

            // Kuryer naqdi: savdo naqd deb smenaga yoziladi, lekin pul kuryerda — shu summaga chiqim (topshirganda kirim).
            if (order.FulfillmentType == FulfillmentType.Delivery && order.PaymentMethod == PaymentMethod.Cash
                && order.PaymentStatus == OrderPaymentStatus.Unpaid)
            {
                var shiftId = await OpenShiftIdAsync(ct);
                _db.CashMovements.Add(new CashMovement
                {
                    OrganizationId = order.OrganizationId,
                    Type = CashMovementType.Expense,
                    Amount = expectedTotal,
                    Comment = $"Online #{order.Number} — naqd kuryerda",
                    BranchId = order.BranchId,
                    CashShiftId = shiftId
                });
                order.CourierCashExpected = true;
            }
        }

        var sale = await _sales.CreateAsync(new CreateSaleRequest
        {
            BranchId = order.BranchId,
            ClientSaleId = clientSaleId,
            TerminalId = request.TerminalId,
            Fiscalize = request.Fiscalize,
            PaymentMethod = order.PaymentMethod,
            TransferSource = order.PaymentMethod == PaymentMethod.Transfer
                ? order.OnlinePaymentProvider?.ToString() ?? "Karta"
                : null,
            CustomerId = order.CustomerId,
            PaidAmount = Math.Round(expectedTotal, 0, MidpointRounding.AwayFromZero),
            SkipPromotions = true,
            Items = saleItems
        }, ct);

        // Bonus tranzaksiyasini savdoga bog'laymiz (SaveAndNotify'da saqlanadi).
        foreach (var tx in _db.LoyaltyTransactions.Local.Where(t => t.SaleId == null && t.Type == LoyaltyTransactionType.Redeem && t.CustomerId == order.CustomerId))
            tx.SaleId = sale.Id;

        var now = DateTime.UtcNow;
        order.SaleId = sale.Id;
        order.ReadyAt = now;
        order.Status = NextAfterReady(order, await RequireTransferBeforeDispatch(order, ct));

        await SaveAndNotify(order, ct);
        // Kuryerlarga taklif (Broadcast rejimida, yetkazish va kuryer biriktirilmagan bo'lsa).
        await _offers.SendAsync(order, manual: false, ct);
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
        var wasAwaiting = order.Status == OnlineOrderStatus.AwaitingTransfer;
        if (wasAwaiting)
            order.Status = order.FulfillmentType == FulfillmentType.Pickup
                ? OnlineOrderStatus.ReadyForPickup
                : OnlineOrderStatus.Ready;

        await SaveAndNotify(order, ct);
        if (wasAwaiting) await _offers.SendAsync(order, manual: false, ct);
        return await GetAsync(id, ct);
    }

    public async Task<OnlineOrderDto> DispatchAsync(Guid id, DispatchOnlineOrderRequest? request = null,
        CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        if (order.Status == OnlineOrderStatus.AwaitingTransfer)
            throw new ValidationException("Avval o'tkazma tushganini tasdiqlang.");
        Require(order, OnlineOrderStatus.Ready);
        if (order.FulfillmentType != FulfillmentType.Delivery)
            throw new ValidationException("Olib ketish buyurtmasi kuryerga berilmaydi.");

        if (request?.CourierUserId is { } courierId)
        {
            // Biriktirilgan kuryer: ism/telefon foydalanuvchi kartochkasidan.
            var courier = await _db.Users.AsNoTracking()
                              .FirstOrDefaultAsync(u => u.Id == courierId && u.Role == UserRole.Courier && u.IsActive, ct)
                          ?? throw new ValidationException("Kuryer topilmadi yoki faol emas.");
            if (courier.BranchId is { } cb && cb != order.BranchId)
                throw new ValidationException("Kuryer boshqa filialga biriktirilgan.");

            order.CourierUserId = courier.Id;
            order.CourierName = Trim(Courier.CourierRules.DisplayName(courier), 100);
            order.CourierPhone = Trim(courier.Phone, 20);
        }
        else
        {
            order.CourierUserId = null;
            order.CourierName = Trim(request?.CourierName, 100);
            order.CourierPhone = Trim(request?.CourierPhone, 20);
        }

        order.Status = OnlineOrderStatus.OnTheWay;
        order.DispatchedAt = DateTime.UtcNow;
        // Qo'lda berildi — kutilayotgan takliflar yopiladi.
        var closedOffers = await _offers.CancelPendingAsync(order.Id, ct);
        if (order.CourierOfferStatus == CourierOfferStatus.Pending)
            order.CourierOfferStatus = order.CourierUserId is null ? CourierOfferStatus.None : CourierOfferStatus.Accepted;

        await SaveAndNotify(order, ct);
        await _notifier.CourierOfferClosedAsync(order.Id, closedOffers, ct);
        if (order.CourierUserId is not null)
            await _notifier.CourierAssignedAsync(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<IReadOnlyList<CourierListItemDto>> GetCouriersAsync(Guid? branchId, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.Role == UserRole.Courier && u.IsActive);
        if ((_currentUser.BranchId ?? branchId) is { } b)
            query = query.Where(u => u.BranchId == b);

        return await query
            .OrderBy(u => u.FullName ?? u.Username)
            .Select(u => new CourierListItemDto
            {
                Id = u.Id,
                Name = u.FullName ?? u.Username,
                Phone = u.Phone,
                ActiveOrders = _db.OnlineOrders.Count(o => o.CourierUserId == u.Id && o.Status == OnlineOrderStatus.OnTheWay)
            })
            .ToListAsync(ct);
    }

    public async Task<OnlineOrderDto> CashHandoverAsync(Guid id, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        if (!order.CourierCashExpected || order.PaymentMethod != PaymentMethod.Cash)
            throw new ValidationException("Bu buyurtma bo'yicha kuryerda naqd yo'q.");
        if (order.CourierCashHandedOverAt is not null)
            throw new ValidationException("Naqd allaqachon topshirilgan.");
        if (order.Status != OnlineOrderStatus.Delivered)
            throw new ValidationException("Buyurtma hali yetkazilmagan.");

        var amount = order.CourierCashCollected ?? order.TotalAmount;
        _db.CashMovements.Add(new CashMovement
        {
            OrganizationId = order.OrganizationId,
            Type = CashMovementType.Income,
            Amount = amount,
            Comment = $"Online #{order.Number} — kuryer topshirdi",
            BranchId = order.BranchId,
            CashShiftId = await OpenShiftIdAsync(ct)
        });
        order.CourierCashHandedOverAt = DateTime.UtcNow;

        await SaveAndNotify(order, ct);
        return await GetAsync(id, ct);
    }

    public async Task<IReadOnlyList<CourierCashItemDto>> GetCourierCashAsync(Guid? branchId, CancellationToken ct = default)
    {
        var query = Scope().AsNoTracking()
            .Where(o => o.CourierCashExpected && o.CourierCashHandedOverAt == null
                        && o.Status == OnlineOrderStatus.Delivered);
        if (_currentUser.BranchId is null && branchId is { } b)
            query = query.Where(o => o.BranchId == b);

        return await query
            .OrderBy(o => o.DeliveredAt)
            .Select(o => new CourierCashItemDto
            {
                OrderId = o.Id,
                Number = o.Number,
                CourierName = o.CourierName,
                Amount = o.CourierCashCollected ?? o.TotalAmount,
                DeliveredAt = o.DeliveredAt
            })
            .ToListAsync(ct);
    }

    public async Task<OnlineOrderDto> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var order = await Load(id, ct);
        if (order.Status is OnlineOrderStatus.New or OnlineOrderStatus.Accepted or OnlineOrderStatus.Picking)
            throw new ValidationException("Yig'ish tugamagan buyurtmani \"Rad etish\" orqali bekor qiling.");
        Require(order, OnlineOrderStatus.Ready, OnlineOrderStatus.AwaitingTransfer,
            OnlineOrderStatus.ReadyForPickup, OnlineOrderStatus.OnTheWay);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Bekor qilish sababini kiriting.");

        order.Status = OnlineOrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = reason.Trim();
        if (order.PaymentStatus == OrderPaymentStatus.Paid)
            order.PaymentStatus = OrderPaymentStatus.Refunded;

        await OnlineOrderPricing.ReleasePromoAsync(_db, order, ct);
        var closedOffers = await _offers.CancelPendingAsync(order.Id, ct);

        // Yechilgan bonus mijozga qaytariladi.
        if (order.BonusCharged && order.BonusUsed > 0 && order.CustomerId is { } customerId
            && await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct) is { } customer)
        {
            LoyaltyRules.Apply(_db, customer, order.BonusUsed, LoyaltyTransactionType.Refund, LoyaltySource.Online,
                order.BonusUsed, $"Online #{order.Number} — bekor qilindi, bonus qaytarildi", saleId: order.SaleId);
            order.BonusCharged = false;
        }

        // "Naqd kuryerda" chiqimi topshirilmasdan qolgan bo'lsa — savdo qaytarilishi (naqd chiqim) bilan ikki marta
        // ayirilmasligi uchun shu summaga kirim yozamiz (smena hisobi to'g'ri qoladi).
        if (order.CourierCashExpected && order.CourierCashHandedOverAt is null && order.Sale?.Status != SaleStatus.Refunded)
        {
            _db.CashMovements.Add(new CashMovement
            {
                OrganizationId = order.OrganizationId,
                Type = CashMovementType.Income,
                Amount = order.Sale?.TotalAmount ?? order.TotalAmount,
                Comment = $"Online #{order.Number} — bekor qilindi (kuryer naqdi tuzatish)",
                BranchId = order.BranchId,
                CashShiftId = await OpenShiftIdAsync(ct)
            });
            order.CourierCashExpected = false;
        }

        // Savdo qaytariladi (ombor qoldig'i refund orqali qaytadi). RefundAsync o'zi SaveChanges qiladi —
        // buyurtma o'zgarishlari ham shu tranzaksiyada saqlanadi. Savdo allaqachon qaytarilgan bo'lsa — faqat holat.
        if (order.SaleId is { } saleId && order.Sale?.Status != SaleStatus.Refunded)
            await _sales.RefundAsync(saleId, ct);
        else
            await _db.SaveChangesAsync(ct);

        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: true, ct);
        await _notifier.CourierOfferClosedAsync(order.Id, closedOffers, ct);
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
            // Naqd kuryerda bo'lsa — topshirish ("cash-handover") kutiladi.
            if (order.PaymentMethod == PaymentMethod.Cash && order.CourierCashExpected)
                order.CourierCashCollected ??= order.TotalAmount;
        }

        order.Status = OnlineOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        // Cashback — mijoz qabul qilgandan keyin (shu tranzaksiyada saqlanadi).
        await OnlineOrderCashback.EarnOnDeliveredAsync(_db, order, ct);
        // Do'stni taklif qilish mukofoti (birinchi yetkazilgan buyurtma, idempotent).
        var referralPushes = await ShopMarketing.ShopReferralRewards.RewardOnDeliveredAsync(_db, order, ct);

        await SaveAndNotify(order, ct);
        foreach (var push in referralPushes) _pushQueue.Enqueue(push);
        return await GetAsync(id, ct);
    }

    // ---------------- yordamchilar ----------------

    private async Task SaveAndNotify(OnlineOrder order, CancellationToken ct)
    {
        // Push faqat holat haqiqatan o'zgarganda (yig'ish tahriri yoki oldindan to'lovda holat o'zgarmaydi).
        var statusChanged = _db.OnlineOrders.Entry(order).Property(o => o.Status).IsModified;
        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: statusChanged, ct);
    }

    /// <summary>Tenant filtri ostidagi buyurtmalar; filialga biriktirilgan xodim — faqat o'z filiali.</summary>
    public const int MaxRatingItems = 500;

    public async Task<OnlineOrderRatingsDto> GetRatingsAsync(DateTime? from, DateTime? to, Guid? branchId,
        CancellationToken ct = default)
    {
        var query = Scope().AsNoTracking().Where(o => o.Rating != null && o.RatedAt != null);

        var effectiveBranch = _currentUser.BranchId ?? branchId;
        if (effectiveBranch is { } b)
            query = query.Where(o => o.BranchId == b);

        // Sana chegaralari UTC kalendar kuni; "to" kuni ham kiradi.
        var since = DateTime.SpecifyKind((from ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        query = query.Where(o => o.RatedAt >= since);
        if (to is { } toDate)
        {
            var until = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(o => o.RatedAt < until);
        }

        var all = await query
            .Select(o => new { o.Rating, o.RatingTags })
            .ToListAsync(ct);
        var items = await query
            .OrderByDescending(o => o.RatedAt)
            .Take(MaxRatingItems)
            .Select(o => new { o.Id, o.Number, o.Rating, o.RatingComment, o.RatingTags, o.RatedAt, o.CustomerName })
            .ToListAsync(ct);

        var dto = new OnlineOrderRatingsDto
        {
            Count = all.Count,
            Average = all.Count > 0 ? Math.Round((decimal)all.Average(x => x.Rating!.Value), 2) : 0,
            Distribution = Enumerable.Range(1, 5).ToDictionary(i => i, i => all.Count(x => x.Rating == i)),
            TagCounts = all.SelectMany(x => OnlineOrderRating.SplitTags(x.RatingTags))
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
            Items = items.Select(x => new OnlineOrderRatingItemDto
            {
                OrderId = x.Id,
                Number = x.Number,
                Rating = x.Rating!.Value,
                Comment = x.RatingComment,
                Tags = OnlineOrderRating.SplitTags(x.RatingTags),
                RatedAt = x.RatedAt!.Value,
                CustomerName = x.CustomerName
            }).ToList()
        };
        return dto;
    }

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

    private static string? Trim(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > max ? s[..max] : s;
    }

    private static void ValidateQuantity(decimal qty, bool isWeighed, string name)
    {
        if (qty <= 0 || qty > 10000 || decimal.Round(qty, 3) != qty || (!isWeighed && qty != Math.Floor(qty)))
            throw new ValidationException($"\"{name}\" miqdori noto'g'ri.");
    }

    /// <summary>
    /// Yig'ilgan miqdor bo'yicha summa. Yetkazish narxi buyurtmadagi qiymatida qoladi.
    /// Promokod chegirmasi yangi mahsulotlar summasidan qayta hisoblanadi (foiz — qayta, summa — ko'pi bilan mahsulotlar summasi),
    /// bonus esa qolgan summadan oshmaydi.
    /// </summary>
    private async Task RecalculateAsync(OnlineOrder order, CancellationToken ct)
    {
        foreach (var i in order.Items)
            i.LineTotal = Round2(i.UnitPrice * OnlineOrderRules.ReservedQuantity(i));

        order.ItemsTotal = order.Items.Sum(i => i.LineTotal);

        if (order.PromoCodeId is { } promoId)
        {
            var promo = await _db.PromoCodes.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == promoId, ct);
            order.PromoDiscount = promo is not null
                ? OnlineOrderPricing.PromoDiscount(promo, order.ItemsTotal)
                : Math.Min(order.PromoDiscount, order.ItemsTotal);
        }
        else
        {
            order.PromoDiscount = Math.Min(order.PromoDiscount, order.ItemsTotal);
        }

        order.BonusUsed = Math.Max(0, Math.Min(order.BonusUsed, Math.Floor(order.ItemsTotal - order.PromoDiscount)));
        order.TotalAmount = order.ItemsTotal - order.PromoDiscount - order.BonusUsed + order.DeliveryFee;
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Yig'ishda kiritilgan markirovka kodlari: bo'sh emas, takrorsiz, soni butun miqdordan oshmaydi.</summary>
    private static List<string> NormalizeMarkingCodes(OnlineOrderItem item, List<string> codes)
    {
        if (item.Product?.IsMarked != true)
        {
            if (codes.Count == 0) return [];
            throw new ValidationException($"\"{item.ProductName}\" markirovkali tovar emas.");
        }

        var list = codes.Select(c => c?.Trim() ?? string.Empty).ToList();
        if (list.Any(c => c.Length == 0 || c.Length > 256))
            throw new ValidationException($"\"{item.ProductName}\" uchun markirovka kodi noto'g'ri.");
        if (list.Count != list.Distinct(StringComparer.Ordinal).Count())
            throw new ValidationException($"\"{item.ProductName}\" uchun bir kod ikki marta skanerlangan.");

        var need = (int)Math.Ceiling(OnlineOrderRules.ReservedQuantity(item));
        if (list.Count > need)
            throw new ValidationException($"\"{item.ProductName}\" uchun ko'pi bilan {need} ta kod kerak.");
        return list;
    }

    /// <summary>Joriy xodimning ochiq smenasi (savdo va kassa harakatlari shu smenaga yoziladi).</summary>
    private async Task<Guid> OpenShiftIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");
        return await _db.CashShifts.AsNoTracking()
                   .Where(s => s.CashierId == userId && s.Status == ShiftStatus.Open)
                   .Select(s => (Guid?)s.Id)
                   .FirstOrDefaultAsync(ct)
               ?? throw new ValidationException("Avval smenani oching (kassa sessiyasi).");
    }

    public const string DeliveryProductName = "Yetkazib berish xizmati";

    /// <summary>
    /// Tashkilotning yagona yashirin "Yetkazib berish xizmati" mahsuloti (ProductType.Service, ShowOnline=false).
    /// Yo'q bo'lsa yaratiladi, MXIK sozlamadan yangilanadi. Darhol saqlanadi (SaleService bazadan o'qiydi).
    /// </summary>
    private async Task<Product> EnsureDeliveryProductAsync(Guid orgId, string mxik, CancellationToken ct)
    {
        var product = await _db.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.ProductType == ProductType.Service, ct);

        if (product is not null)
        {
            if (product.MxikCode == mxik && !product.ShowOnline) return product;
            product.MxikCode = mxik;
            product.ShowOnline = false;
            await _db.SaveChangesAsync(ct);
            return product;
        }

        const string categoryName = "Xizmatlar";
        var category = await _db.Categories.IgnoreQueryFilters()
                           .FirstOrDefaultAsync(c => c.OrganizationId == orgId && c.Name == categoryName, ct)
                       ?? _db.Categories.Add(new Category
                       {
                           OrganizationId = orgId,
                           Name = categoryName,
                           Note = "Avtomatik: online yetkazish xizmati"
                       }).Entity;

        product = new Product
        {
            OrganizationId = orgId,
            Name = DeliveryProductName,
            ProductType = ProductType.Service,
            MxikCode = mxik,
            Price = 0,
            CostPrice = 0,
            ShowOnline = false,
            Unit = Unit.Piece,
            Category = category,
            CategoryId = category.Id
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
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
