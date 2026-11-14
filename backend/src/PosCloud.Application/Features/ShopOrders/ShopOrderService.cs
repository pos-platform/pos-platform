using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.OnlineReturns;
using PosCloud.Application.Features.OnlinePayments;
using PosCloud.Application.Features.OnlinePayments.Dtos;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopOrders.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopOrders;

public interface IShopOrderService
{
    /// <param name="shopUserId">Kirgan mijoz (ixtiyoriy) — bonus, promokod limiti va manzil (hudud) uchun.</param>
    Task<ShopOrderQuoteDto> QuoteAsync(ShopOrderQuoteRequest request, Guid? shopUserId = null, CancellationToken ct = default);

    /// <summary>Online to'lov havolasi (Payme/Click) — mavjud OnlinePaymentService orqali, savdo (SaleId) bo'yicha.</summary>
    Task<PayShopOrderResponse> PayAsync(Guid shopUserId, Guid orderId, PaymentProvider provider, CancellationToken ct = default);

    /// <summary>Mijozning do'kondagi bonus balli.</summary>
    Task<ShopBonusDto> GetBonusAsync(Guid shopUserId, Guid branchId, CancellationToken ct = default);
    Task<ShopOrderDto> CreateAsync(Guid shopUserId, CreateShopOrderRequest request, CancellationToken ct = default);
    Task<PagedResult<ShopOrderSummaryDto>> GetListAsync(Guid shopUserId, bool? active, int page, int pageSize, CancellationToken ct = default);
    Task<ShopOrderDto> GetAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default);
    Task<ShopOrderDto> CancelAsync(Guid shopUserId, Guid orderId, string? reason, CancellationToken ct = default);
    Task<ShopOrderDto> SetTransferProofAsync(Guid shopUserId, Guid orderId, string url, CancellationToken ct = default);

    /// <summary>Yetkazilgan buyurtmani baholash (faqat canRate).</summary>
    Task<ShopOrderDto> RateAsync(Guid shopUserId, Guid orderId, RateShopOrderRequest request, CancellationToken ct = default);

    /// <summary>Chek yuklashdan oldin tekshiruv (fayl saqlanmasidan avval).</summary>
    Task EnsureCanUploadTransferProofAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default);
}

/// <summary>
/// Mijoz buyurtmalari. Tenant filtri yo'q — har so'rovda ShopUserId, tashkilot va filial QO'LDA filtrlanadi.
/// Narx, aksiya, qoldiq, yetkazish narxi va jami faqat shu yerda hisoblanadi (ilovadan kelgan narx qabul qilinmaydi).
/// Ombor rezervi buyurtma yaratilganda emas, do'kon qabul qilganda qo'yiladi (1.7).
/// </summary>
public class ShopOrderService : IShopOrderService
{
    public const int MaxItems = 100;
    public const int MaxActiveOrders = 5;
    private const int MaxPageSize = 50;
    private static readonly TimeSpan MinScheduleLead = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MaxScheduleAhead = TimeSpan.FromDays(2);

    private readonly IApplicationDbContext _db;
    private readonly IOnlineOrderNotifier _notifier;
    private readonly IOnlinePaymentService _payments;

    public ShopOrderService(IApplicationDbContext db, IOnlineOrderNotifier notifier, IOnlinePaymentService payments)
    {
        _db = db;
        _notifier = notifier;
        _payments = payments;
    }

    public async Task<ShopOrderQuoteDto> QuoteAsync(ShopOrderQuoteRequest request, Guid? shopUserId = null,
        CancellationToken ct = default)
    {
        var store = await RequireStore(request.BranchId, ct);
        return (await Calculate(store, request, shopUserId, ct)).Quote;
    }

    public async Task<PayShopOrderResponse> PayAsync(Guid shopUserId, Guid orderId, PaymentProvider provider,
        CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var order = await Orders(shopUserId).AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == order.OrganizationId, ct);

        if (!Enum.IsDefined(provider) || !OnlineProviders(settings).Contains(provider.ToString()))
            throw new ValidationException("Bu to'lov tizimi ushbu do'konda mavjud emas.");
        if (!CanPayOnline(order, settings))
            throw new ValidationException(order.PaymentStatus != OrderPaymentStatus.Unpaid
                ? "Buyurtma allaqachon to'langan."
                : "Online to'lov buyurtma yig'ilgandan keyin (yakuniy summa ma'lum bo'lgach) mumkin.");

        // Summa — buyurtmaning yakuniy summasi (yetkazish narxi chekka kirmagan bo'lsa ham to'liq olinadi).
        var payment = await _payments.CreateAsync(new CreateOnlinePaymentRequest
        {
            SaleId = order.SaleId!.Value,
            Provider = provider,
            AmountOverride = order.TotalAmount
        }, ct);

        if (string.IsNullOrWhiteSpace(payment.PaymentUrl))
            throw new ValidationException("To'lov havolasini yaratib bo'lmadi. Keyinroq urinib ko'ring.");
        return new PayShopOrderResponse { PaymentUrl = payment.PaymentUrl };
    }

    public async Task<ShopBonusDto> GetBonusAsync(Guid shopUserId, Guid branchId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var orgId = await _db.Branches.IgnoreQueryFilters().AsNoTracking()
                        .Where(b => b.Id == branchId).Select(b => b.OrganizationId).FirstOrDefaultAsync(ct)
                    ?? throw new NotFoundException("Do'kon topilmadi.");
        var points = await OnlineOrderPricing.CustomerPointsAsync(_db, orgId, shopUserId, ct);
        return new ShopBonusDto { Points = Math.Floor(points) };
    }

    public async Task<ShopOrderDto> CreateAsync(Guid shopUserId, CreateShopOrderRequest request, CancellationToken ct = default)
    {
        var user = await RequireUser(shopUserId, ct);

        // Idempotentlik: shu kalit bilan buyurtma allaqachon yaratilgan bo'lsa — o'shani qaytaramiz.
        var clientRequestId = NullIfEmpty(request.ClientRequestId);
        if (clientRequestId is { Length: > 64 })
            throw new ValidationException("ClientRequestId juda uzun.");
        if (clientRequestId is not null && await FindByClientRequestId(shopUserId, clientRequestId, ct) is { } existingId)
            return await GetAsync(shopUserId, existingId, ct);

        var store = await RequireStore(request.BranchId, ct);
        var settings = store.Settings;
        var orgId = store.Branch.OrganizationId!.Value;

        var activeCount = await _db.OnlineOrders.IgnoreQueryFilters()
            .CountAsync(o => o.ShopUserId == shopUserId && !OnlineOrderRules.FinishedStatuses.Contains(o.Status), ct);
        if (activeCount >= MaxActiveOrders)
            throw new ValidationException($"Bir vaqtda ko'pi bilan {MaxActiveOrders} ta faol buyurtma berish mumkin.");

        ValidatePayment(settings, request.PaymentMethod, request.OnlinePaymentProvider);
        if (!Enum.IsDefined(request.OutOfStockPolicy))
            throw new ValidationException("Mahsulot tugasa nima qilish kerakligini tanlang.");

        var (scheduledFor, isPreorder) = ValidateSchedule(settings, request.ScheduledFor);

        var (quote, promo) = await Calculate(store, request, shopUserId, ct);
        if (OnlineOrderPricing.NormalizeCode(request.PromoCode) is not null && quote.PromoMessage is not null)
            throw new ValidationException(quote.PromoMessage);
        if (!quote.DeliveryAvailable)
            throw new ValidationException(quote.DeliveryMessage ?? "Manzil yetkazish hududidan tashqarida.");
        if (!quote.CanOrder)
        {
            if (quote.Message is not null) throw new ValidationException(quote.Message);
            var bad = quote.Lines.FirstOrDefault(l => l.Problem is not null);
            if (bad is not null) throw new ValidationException(ProblemText(bad));
            throw new ValidationException("Buyurtmani rasmiylashtirib bo'lmadi.");
        }

        var order = new OnlineOrder
        {
            OrganizationId = orgId,
            BranchId = store.Branch.Id,
            ShopUserId = shopUserId,
            CustomerName = FullName(user),
            CustomerPhone = user.Phone,
            Status = OnlineOrderStatus.New,
            FulfillmentType = request.FulfillmentType,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = OrderPaymentStatus.Unpaid,
            ScheduledFor = scheduledFor,
            IsPreorder = isPreorder,
            Contactless = request.FulfillmentType == FulfillmentType.Delivery && request.Contactless,
            Comment = NullIfEmpty(request.Comment),
            OutOfStockPolicy = request.OutOfStockPolicy,
            ClientRequestId = clientRequestId,
            ItemsTotal = quote.ItemsTotal,
            DeliveryFee = quote.DeliveryFee,
            TotalAmount = quote.TotalAmount,
            PromoCodeId = promo?.Id,
            PromoCode = promo?.Code,
            PromoDiscount = quote.PromoDiscount,
            BonusUsed = quote.BonusUsed,
            OnlinePaymentProvider = request.PaymentMethod == PaymentMethod.Transfer ? request.OnlinePaymentProvider : null
        };

        // Promokod ishlatilishi buyurtma bilan bitta SaveChanges'da saqlanadi (bekor/rad etilganda qaytariladi).
        if (promo is not null)
            promo.UsedCount++;

        if (request.FulfillmentType == FulfillmentType.Delivery)
        {
            if (request.AddressId is not { } addressId)
                throw new ValidationException("Yetkazish manzilini tanlang.");

            var address = await _db.ShopUserAddresses.IgnoreQueryFilters().AsNoTracking()
                              .FirstOrDefaultAsync(a => a.Id == addressId && a.ShopUserId == shopUserId, ct)
                          ?? throw new ValidationException("Manzil topilmadi.");

            order.AddressLine = address.AddressLine;
            order.Entrance = address.Entrance;
            order.Floor = address.Floor;
            order.Apartment = address.Apartment;
            order.Intercom = address.Intercom;
            order.Landmark = address.Landmark;
            order.Latitude = address.Latitude;
            order.Longitude = address.Longitude;
            // Manzil izohi va buyurtma izohini birlashtiramiz (kuryer bitta joyda ko'radi).
            if (!string.IsNullOrWhiteSpace(address.Comment) && order.Comment is null)
                order.Comment = address.Comment;
        }

        foreach (var line in quote.Lines)
        {
            order.Items.Add(new OnlineOrderItem
            {
                OrganizationId = orgId,
                ProductId = line.ProductId,
                ProductName = line.Name,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                LineTotal = line.LineTotal
            });
        }

        order.CustomerId = await LinkCustomer(orgId, user, ct);

        _db.OnlineOrders.Add(order);
        if (await SaveWithNumber(order, orgId, ct) is { } duplicateId)
            return await GetAsync(shopUserId, duplicateId, ct); // parallel so'rov avvalroq yaratgan

        await _notifier.OrderChangedAsync(order, isNew: true, pushToCustomer: false, ct);

        return await GetAsync(shopUserId, order.Id, ct);
    }

    public async Task<PagedResult<ShopOrderSummaryDto>> GetListAsync(Guid shopUserId, bool? active, int page, int pageSize,
        CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = Orders(shopUserId).AsNoTracking();
        if (active == true)
            query = query.Where(o => !OnlineOrderRules.FinishedStatuses.Contains(o.Status));
        else if (active == false)
            query = query.Where(o => OnlineOrderRules.FinishedStatuses.Contains(o.Status));

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .ToListAsync(ct);

        var names = await StoreNames(orders.Select(o => o.OrganizationId!.Value).Distinct().ToList(), ct);

        return new PagedResult<ShopOrderSummaryDto>
        {
            Items = orders.Select(o => FillSummary(new ShopOrderSummaryDto(), o, names)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ShopOrderDto> GetAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var order = await Orders(shopUserId).AsNoTracking()
                        .Include(o => o.Items).ThenInclude(i => i.Product)
                        .Include(o => o.Branch)
                        .Include(o => o.Sale).ThenInclude(s => s!.Receipt)
                        .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");

        var orgId = order.OrganizationId!.Value;
        var names = await StoreNames([orgId], ct);
        var settings = await _db.OnlineStoreSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        var dto = FillSummary(new ShopOrderDto(), order, names);
        dto.BranchName = order.Branch?.Name;
        dto.BranchAddress = order.Branch?.Address;
        dto.StorePhone = settings?.Phone;
        dto.AddressLine = order.AddressLine;
        dto.Entrance = order.Entrance;
        dto.Floor = order.Floor;
        dto.Apartment = order.Apartment;
        dto.Intercom = order.Intercom;
        dto.Landmark = order.Landmark;
        dto.Latitude = order.Latitude;
        dto.Longitude = order.Longitude;
        dto.ScheduledFor = order.ScheduledFor;
        dto.Contactless = order.Contactless;
        dto.Comment = order.Comment;
        dto.OutOfStockPolicy = order.OutOfStockPolicy;
        dto.ItemsTotal = order.ItemsTotal;
        dto.DeliveryFee = order.DeliveryFee;
        dto.Items = order.Items
            .OrderBy(i => i.CreatedAt)
            .Select(i => new ShopOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ImageUrl = i.Product?.ImageUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                PickedQuantity = i.PickedQuantity,
                LineTotal = i.LineTotal,
                IsRemoved = i.IsRemoved,
                ReplacesItemId = i.ReplacesItemId,
                RejectedQuantity = i.RejectedQuantity,
                ReturnedQuantity = i.ReturnedQuantity,
                ReturnableQuantity = order.Status == OnlineOrderStatus.Delivered ? OnlineReturnRules.Returnable(i) : 0
            })
            .ToList();
        dto.IsPreorder = order.IsPreorder;
        dto.CanReturn = OnlineReturnRules.CanReturn(order, settings, DateTime.UtcNow);
        dto.AcceptedAt = order.AcceptedAt;
        dto.ReadyAt = order.ReadyAt;
        dto.DispatchedAt = order.DispatchedAt;
        dto.DeliveredAt = order.DeliveredAt;
        dto.CancelledAt = order.CancelledAt;
        dto.CancelReason = order.CancelReason;
        dto.PaymentConfirmedAt = order.PaymentConfirmedAt;
        dto.PickingStartedAt = order.PickingStartedAt;
        dto.CourierName = order.CourierName;
        dto.CourierPhone = order.CourierPhone;
        dto.ReceiptNumber = order.Sale?.ReceiptNumber;
        dto.FiscalQrUrl = string.IsNullOrWhiteSpace(order.Sale?.Receipt?.QrCodeUrl) ? null : order.Sale!.Receipt!.QrCodeUrl;
        (dto.EstimatedDeliveryFrom, dto.EstimatedDeliveryTo) = EstimateDelivery(order, settings);
        dto.CanCancel = OnlineOrderRules.CustomerCanCancel(order.Status);
        dto.CanUploadTransferProof = CanUploadProof(order);
        dto.TransferProofUrl = order.TransferProofUrl;

        if (order.PaymentMethod == PaymentMethod.Transfer && order.OnlinePaymentProvider is null && ShowTransferCard(order) && settings is not null)
        {
            dto.TransferCardNumber = settings.TransferCardNumber;
            dto.TransferCardHolder = settings.TransferCardHolder;
        }

        dto.PromoCode = order.PromoCode;
        dto.PromoDiscount = order.PromoDiscount;
        dto.BonusUsed = order.BonusUsed;
        dto.CashbackEarned = order.Status == OnlineOrderStatus.Delivered && order.Sale is { } deliveredSale
            ? deliveredSale.LoyaltyPointsEarned
            : null;
        dto.OnlinePaymentProvider = order.OnlinePaymentProvider;
        dto.OnlineProviders = OnlineProviders(settings);
        dto.CanPayOnline = CanPayOnline(order, settings);
        dto.Rating = order.Rating;
        dto.RatingComment = order.RatingComment;
        dto.RatingTags = OnlineOrderRating.SplitTags(order.RatingTags);
        dto.RatedAt = order.RatedAt;
        dto.CanRate = OnlineOrderRating.CanRate(order);

        return dto;
    }

    public async Task<ShopOrderDto> RateAsync(Guid shopUserId, Guid orderId, RateShopOrderRequest request,
        CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var order = await Orders(shopUserId).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");

        if (!OnlineOrderRating.CanRate(order))
            throw new ValidationException(order.Rating is not null
                ? "Buyurtma allaqachon baholangan."
                : "Faqat yetkazilgan buyurtmani 14 kun ichida baholash mumkin.");
        if (request.Rating is < 1 or > 5)
            throw new ValidationException("Baho 1 dan 5 gacha bo'lishi kerak.");

        var tags = (request.Tags ?? [])
            .Select(t => (t ?? string.Empty).Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();
        if (tags.Any(t => !OnlineOrderRating.AllowedTags.Contains(t)))
            throw new ValidationException("Noma'lum teg.");

        var comment = NullIfEmpty(request.Comment);
        if (comment is { Length: > 500 })
            throw new ValidationException("Izoh 500 belgidan oshmasligi kerak.");

        order.Rating = request.Rating;
        order.RatingComment = comment;
        order.RatingTags = tags.Count > 0 ? string.Join(",", tags) : null;
        order.RatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        // Xodimlarga SignalR OrderUpdated (push yo'q).
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        return await GetAsync(shopUserId, orderId, ct);
    }

    public async Task<ShopOrderDto> CancelAsync(Guid shopUserId, Guid orderId, string? reason, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);

        var order = await Orders(shopUserId)
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");

        if (!OnlineOrderRules.CustomerCanCancel(order.Status))
            throw new ValidationException(order.Status == OnlineOrderStatus.Cancelled
                ? "Buyurtma allaqachon bekor qilingan."
                : "Yig'ish boshlangani uchun bekor qilib bo'lmaydi. Do'konga qo'ng'iroq qiling.");

        if (OnlineOrderRules.HoldsReservation(order.Status))
            await OnlineOrderRules.AdjustReservationAsync(_db, order, -1, ct);

        order.Status = OnlineOrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = NullIfEmpty(reason) ?? "Mijoz bekor qildi";

        await OnlineOrderPricing.ReleasePromoAsync(_db, order, ct);
        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        return await GetAsync(shopUserId, orderId, ct);
    }

    public async Task EnsureCanUploadTransferProofAsync(Guid shopUserId, Guid orderId, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var order = await Orders(shopUserId).AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        EnsureCanUpload(order);
    }

    public async Task<ShopOrderDto> SetTransferProofAsync(Guid shopUserId, Guid orderId, string url, CancellationToken ct = default)
    {
        await RequireUser(shopUserId, ct);
        var order = await Orders(shopUserId).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Buyurtma topilmadi.");
        EnsureCanUpload(order);

        order.TransferProofUrl = url;
        await _db.SaveChangesAsync(ct);
        await _notifier.OrderChangedAsync(order, isNew: false, pushToCustomer: false, ct);
        return await GetAsync(shopUserId, orderId, ct);
    }

    // ---------------- hisob-kitob ----------------

    private async Task<(ShopOrderQuoteDto Quote, PromoCode? Promo)> Calculate(ShopCatalogService.StoreRow store,
        ShopOrderQuoteRequest request, Guid? shopUserId, CancellationToken ct)
    {
        var s = store.Settings;
        var branchId = store.Branch.Id;
        var orgId = store.Branch.OrganizationId!.Value;

        if (!Enum.IsDefined(request.FulfillmentType))
            throw new ValidationException("Qabul qilish usulini tanlang.");
        if (request.Items.Count == 0)
            throw new ValidationException("Savatcha bo'sh.");

        // Bir mahsulot bir necha marta kelsa — qo'shamiz.
        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();
        if (requested.Count > MaxItems)
            throw new ValidationException($"Bitta buyurtmada ko'pi bilan {MaxItems} xil mahsulot bo'lishi mumkin.");

        var ids = requested.Select(r => r.ProductId).ToList();
        var products = await ShopCatalogService.VisibleProductsQuery(_db, orgId, branchId).AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                Product = p,
                Available = p.Stocks.Where(st => st.BranchId == branchId).Select(st => st.Quantity - st.Reserved).FirstOrDefault()
            })
            .ToListAsync(ct);
        var promotions = await ShopCatalogService.LoadActivePromotionsAsync(_db, orgId, ct);

        var quote = new ShopOrderQuoteDto
        {
            BranchId = branchId,
            MinOrderAmount = s.MinOrderAmount
        };

        foreach (var (productId, qty) in requested)
        {
            var row = products.FirstOrDefault(p => p.Product.Id == productId);
            if (row is null)
            {
                quote.Lines.Add(new ShopOrderQuoteLineDto { ProductId = productId, Quantity = qty, Problem = "not_found" });
                continue;
            }

            var p = row.Product;
            var percent = PromotionCalculator.GetDiscountPercent(promotions, p.Id, p.CategoryId);
            var price = ShopCatalogService.DiscountedPrice(p.Price, percent);
            var available = Math.Max(0, row.Available);
            var line = new ShopOrderQuoteLineDto
            {
                ProductId = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                Unit = p.Unit,
                IsWeighed = p.IsWeighed,
                Quantity = qty,
                UnitPrice = price,
                OldPrice = percent > 0 ? p.Price : null,
                LineTotal = Math.Round(price * qty, 2, MidpointRounding.AwayFromZero),
                Available = available
            };

            if (qty <= 0 || qty > 10000 || (!p.IsWeighed && qty != Math.Floor(qty)) || decimal.Round(qty, 3) != qty)
                line.Problem = "invalid_quantity";
            else if (available <= 0)
                line.Problem = "out_of_stock";
            else if (qty > available)
                line.Problem = "not_enough";

            quote.Lines.Add(line);
        }

        var okLines = quote.Lines.Where(l => l.Problem is null).ToList();
        quote.ItemsTotal = okLines.Sum(l => l.LineTotal);

        if (request.FulfillmentType == FulfillmentType.Delivery)
        {
            var free = s.FreeDeliveryFrom is { } from && quote.ItemsTotal >= from;
            quote.DeliveryFee = free ? 0 : s.DeliveryFee;
            quote.FreeDeliveryFrom = s.FreeDeliveryFrom;
            quote.FreeDeliveryRemaining = s.FreeDeliveryFrom is { } f ? Math.Max(0, f - quote.ItemsTotal) : null;
        }

        // Promokod (mahsulotlar summasidan).
        var promoCheck = await OnlineOrderPricing.CheckPromoAsync(_db, orgId, request.PromoCode, quote.ItemsTotal, shopUserId, ct);
        quote.PromoDiscount = promoCheck.Discount;
        quote.PromoMessage = promoCheck.Message;

        // Bonus: faqat kirgan mijoz, do'kondagi ball bo'yicha.
        if (shopUserId is { } userId)
        {
            var points = await OnlineOrderPricing.CustomerPointsAsync(_db, orgId, userId, ct);
            quote.BonusAvailable = Math.Floor(points);
            if (request.UseBonus)
                quote.BonusUsed = OnlineOrderPricing.BonusCap(s, quote.ItemsTotal, quote.PromoDiscount, points);
        }

        // Yetkazish hududi (manzil koordinatasi va filial koordinatasi bo'yicha).
        if (request.FulfillmentType == FulfillmentType.Delivery && request.AddressId is { } addressId && shopUserId is { } uid)
        {
            var address = await _db.ShopUserAddresses.IgnoreQueryFilters().AsNoTracking()
                .Where(a => a.Id == addressId && a.ShopUserId == uid)
                .Select(a => new { a.Latitude, a.Longitude })
                .FirstOrDefaultAsync(ct);
            if (address is not null)
            {
                var area = OnlineOrderPricing.CheckDeliveryArea(s, store.Branch, address.Latitude, address.Longitude);
                quote.DeliveryAvailable = area.Available;
                quote.DeliveryMessage = area.Message;
                quote.DistanceKm = area.DistanceKm;
            }
        }

        quote.TotalAmount = quote.ItemsTotal - quote.PromoDiscount - quote.BonusUsed + quote.DeliveryFee;
        quote.MinOrderShortfall = Math.Max(0, s.MinOrderAmount - quote.ItemsTotal);

        quote.Message = StoreBlocker(s, request.FulfillmentType);
        if (quote.Message is null && quote.MinOrderShortfall > 0)
            quote.Message = $"Minimal buyurtma summasi {s.MinOrderAmount:N0} so'm. Yana {quote.MinOrderShortfall:N0} so'mlik mahsulot qo'shing.";

        quote.CanOrder = quote.Message is null && quote.DeliveryAvailable
                         && okLines.Count == quote.Lines.Count && okLines.Count > 0;

        // Ish vaqtidan tashqari: AcceptWhenClosed bo'lsa — buyurtma qabul qilinadi (oldindan buyurtma), faqat izoh.
        quote.AcceptWhenClosed = s.AcceptWhenClosed;
        var now = DateTime.UtcNow;
        if (!ShopCatalogService.IsWithinHours(s.OpensAt, s.ClosesAt, now))
        {
            quote.NextOpenAt = ShopCatalogService.NextOpenAt(s.OpensAt, s.ClosesAt, now);
            if (s.AcceptWhenClosed && quote.Message is null)
                quote.Message = ClosedMessage(s);
        }
        return (quote, promoCheck.Promo);
    }

    /// <summary>Do'kon darajasidagi to'siq (yopiq, qabul o'chirilgan, usul yo'q). Null — to'siq yo'q.</summary>
    private static string? StoreBlocker(OnlineStoreSettings s, FulfillmentType type)
    {
        if (!s.IsAcceptingOrders)
            return "Do'kon hozir buyurtma qabul qilmayapti.";
        if (type == FulfillmentType.Delivery && !s.DeliveryEnabled)
            return "Bu do'kon yetkazib bermaydi. Olib ketishni tanlang.";
        if (type == FulfillmentType.Pickup && !s.PickupEnabled)
            return "Bu do'konda olib ketish yo'q. Yetkazishni tanlang.";
        return null;
    }

    private static void ValidatePayment(OnlineStoreSettings s, PaymentMethod method, PaymentProvider? provider)
    {
        if (provider is not null && method != PaymentMethod.Transfer)
            throw new ValidationException("Payme/Click faqat \"Transfer\" to'lov usuli bilan tanlanadi.");

        var allowed = method switch
        {
            PaymentMethod.Cash => s.AllowCash,
            // Payme/Click (yig'ilgach) — karta raqami shart emas, do'konda shu provayder yoqilgan bo'lsa yetarli.
            PaymentMethod.Transfer when provider is PaymentProvider.Payme => s.AllowPayme,
            PaymentMethod.Transfer when provider is PaymentProvider.Click => s.AllowClick,
            PaymentMethod.Transfer when provider is not null => false,
            PaymentMethod.Transfer => s.AllowTransfer && !string.IsNullOrWhiteSpace(s.TransferCardNumber),
            PaymentMethod.Card => s.AllowCardOnDelivery,
            _ => false
        };
        if (!allowed)
            throw new ValidationException("Bu to'lov usuli ushbu do'konda mavjud emas.");
    }

    /// <summary>
    /// Null — imkon qadar tez: do'kon ochiq bo'lsa — null; yopiq va AcceptWhenClosed bo'lsa — oldindan buyurtma
    /// (ScheduledFor = keyingi ochilish vaqti, IsPreorder). Aks holda 30 daqiqadan 2 kungacha, ish vaqtida.
    /// </summary>
    private static (DateTime? At, bool IsPreorder) ValidateSchedule(OnlineStoreSettings s, DateTime? scheduledFor)
    {
        var now = DateTime.UtcNow;
        if (scheduledFor is null)
        {
            if (!ShopCatalogService.IsWithinHours(s.OpensAt, s.ClosesAt, now))
            {
                if (s.AcceptWhenClosed && ShopCatalogService.NextOpenAt(s.OpensAt, s.ClosesAt, now) is { } nextOpen)
                    return (nextOpen, true);
                throw new ValidationException($"Do'kon hozir yopiq ({s.OpensAt:HH\\:mm}–{s.ClosesAt:HH\\:mm}). Vaqtni tanlab buyurtma bering.");
            }
            return (null, false);
        }

        var at = DateTime.SpecifyKind(scheduledFor.Value, DateTimeKind.Utc);
        if (scheduledFor.Value.Kind == DateTimeKind.Local) at = scheduledFor.Value.ToUniversalTime();

        if (at < now + MinScheduleLead || at > now + MaxScheduleAhead)
            throw new ValidationException("Vaqtni kamida 30 daqiqa keyinga va ko'pi bilan 2 kun ichida tanlang.");
        if (!ShopCatalogService.IsWithinHours(s.OpensAt, s.ClosesAt, at))
            throw new ValidationException("Tanlangan vaqtda do'kon ishlamaydi.");
        return (at, false);
    }

    /// <summary>Yopiq do'kon xabari: "Do'kon yopiq. Buyurtma 09:00 da ko'rib chiqiladi".</summary>
    public static string ClosedMessage(OnlineStoreSettings s) =>
        $"Do'kon yopiq. Buyurtma {s.OpensAt:HH\\:mm} da ko'rib chiqiladi";

    private static string ProblemText(ShopOrderQuoteLineDto l) => l.Problem switch
    {
        "not_found" => "Savatchadagi ba'zi mahsulotlar endi sotuvda yo'q. Savatchani yangilang.",
        "out_of_stock" => $"\"{l.Name}\" tugagan.",
        "not_enough" => $"\"{l.Name}\" dan faqat {l.Available:0.###} mavjud.",
        _ => $"\"{l.Name}\" miqdori noto'g'ri."
    };

    // ---------------- yordamchilar ----------------

    private IQueryable<OnlineOrder> Orders(Guid shopUserId) =>
        _db.OnlineOrders.IgnoreQueryFilters().Where(o => o.ShopUserId == shopUserId);

    private async Task<ShopCatalogService.StoreRow> RequireStore(Guid branchId, CancellationToken ct) =>
        (await ShopCatalogService.LoadVisibleStoresAsync(_db, branchId, ct)).FirstOrDefault()
        ?? throw new NotFoundException("Do'kon topilmadi yoki vaqtincha ishlamayapti.");

    private async Task<ShopUser> RequireUser(Guid shopUserId, CancellationToken ct)
    {
        var user = await _db.ShopUsers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Id == shopUserId, ct);
        if (user is null || !user.IsActive || user.DeletedAt is not null)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        return user;
    }

    /// <summary>Do'kondagi mijoz kartochkasi — umumiy <see cref="ShopCustomerLinker"/> orqali (signupBonus bir marta).</summary>
    private async Task<Guid> LinkCustomer(Guid orgId, ShopUser user, CancellationToken ct) =>
        (await ShopCustomerLinker.EnsureAsync(_db, orgId, user, LoyaltySource.Online, ct)).Id;

    /// <summary>
    /// Tashkilot ichida ketma-ket raqam. Bir vaqtda ikki buyurtma bir raqam olsa — unikal indeks rad etadi, qayta urinamiz.
    /// ClientRequestId bo'yicha parallel dublikat bo'lsa — saqlanmaydi, avvalgi buyurtma ID'si qaytariladi.
    /// </summary>
    /// <returns>Null — saqlandi; aks holda shu kalit bilan mavjud buyurtma ID'si.</returns>
    private async Task<Guid?> SaveWithNumber(OnlineOrder order, Guid orgId, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var last = await _db.OnlineOrders.IgnoreQueryFilters()
                .Where(o => o.OrganizationId == orgId)
                .MaxAsync(o => (int?)o.Number, ct) ?? 1000;
            order.Number = last + 1;

            try
            {
                await _db.SaveChangesAsync(ct);
                return null;
            }
            catch (DbUpdateException)
            {
                if (order.ClientRequestId is { } key
                    && await FindByClientRequestId(order.ShopUserId, key, ct) is { } existingId)
                {
                    // Yangi yozuvlarni kuzatuvdan chiqaramiz — keyingi SaveChanges ularni qayta yozmasin.
                    foreach (var item in order.Items)
                        _db.OnlineOrderItems.Entry(item).State = EntityState.Detached;
                    _db.OnlineOrders.Entry(order).State = EntityState.Detached;
                    return existingId;
                }
                if (attempt >= 5) throw;
                // raqam band bo'ldi — keyingisini olamiz
            }
        }
    }

    private Task<Guid?> FindByClientRequestId(Guid shopUserId, string clientRequestId, CancellationToken ct) =>
        _db.OnlineOrders.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.ShopUserId == shopUserId && o.ClientRequestId == clientRequestId)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<Dictionary<Guid, string>> StoreNames(List<Guid> orgIds, CancellationToken ct)
    {
        var rows = await _db.Organizations.AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
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

    private static T FillSummary<T>(T dto, OnlineOrder o, Dictionary<Guid, string> names) where T : ShopOrderSummaryDto
    {
        var items = o.Items.Where(i => !i.IsRemoved).ToList();
        dto.Id = o.Id;
        dto.Number = o.Number;
        dto.BranchId = o.BranchId;
        dto.StoreName = names.GetValueOrDefault(o.OrganizationId!.Value, string.Empty);
        dto.Status = o.Status;
        dto.FulfillmentType = o.FulfillmentType;
        dto.PaymentMethod = o.PaymentMethod;
        dto.PaymentStatus = o.PaymentStatus;
        dto.TotalAmount = o.TotalAmount;
        dto.ItemCount = items.Count;
        dto.PreviewImages = items.OrderBy(i => i.CreatedAt).Take(3).Select(i => i.Product?.ImageUrl).ToList();
        dto.CreatedAt = o.CreatedAt;
        dto.IsActive = !OnlineOrderRules.IsFinished(o.Status);
        return dto;
    }

    /// <summary>
    /// Taxminiy yetkazish oralig'i (UTC). Faqat yetkazish, tugamagan buyurtma va do'kon min/max kiritgan bo'lsa.
    /// Vaqt tanlangan bo'lsa — tanlangan vaqtdan +60 daqiqa; aks holda (qabul qilingan yoki yaratilgan vaqt) + min..max.
    /// </summary>
    private static (DateTime? From, DateTime? To) EstimateDelivery(OnlineOrder o, OnlineStoreSettings? s)
    {
        if (o.FulfillmentType != FulfillmentType.Delivery || OnlineOrderRules.IsFinished(o.Status)
            || s?.DeliveryMinMinutes is not { } min || s.DeliveryMaxMinutes is not { } max)
            return (null, null);

        if (o.ScheduledFor is { } at)
            return (at, at.AddMinutes(60));

        var start = o.AcceptedAt ?? o.CreatedAt;
        return (start.AddMinutes(min), start.AddMinutes(max));
    }

    /// <summary>Karta raqami yakuniy summa ma'lum bo'lgach (yig'ish tugagach) ko'rsatiladi.</summary>
    private static bool ShowTransferCard(OnlineOrder o) =>
        o.PaymentStatus == OrderPaymentStatus.Unpaid
        && o.Status is OnlineOrderStatus.Ready or OnlineOrderStatus.AwaitingTransfer
            or OnlineOrderStatus.OnTheWay or OnlineOrderStatus.ReadyForPickup;

    private static bool CanUploadProof(OnlineOrder o) =>
        o.PaymentMethod == PaymentMethod.Transfer && o.OnlinePaymentProvider is null && ShowTransferCard(o);

    /// <summary>Do'kon ruxsat bergan online to'lov tizimlari.</summary>
    private static List<string> OnlineProviders(OnlineStoreSettings? s)
    {
        var list = new List<string>();
        if (s?.AllowPayme == true) list.Add(nameof(PaymentProvider.Payme));
        if (s?.AllowClick == true) list.Add(nameof(PaymentProvider.Click));
        return list;
    }

    /// <summary>
    /// Online to'lash mumkin: do'kon Payme/Click ruxsat bergan, buyurtma to'lanmagan, yig'ilgan (Ready..OnTheWay), savdo bor.
    /// Faqat Transfer usulida (naqd/terminal buyurtma kuryer orqali yopiladi — kassa hisobi buzilmasin).
    /// </summary>
    private static bool CanPayOnline(OnlineOrder o, OnlineStoreSettings? s) =>
        OnlineProviders(s).Count > 0
        && o.PaymentMethod == PaymentMethod.Transfer
        && o.PaymentStatus == OrderPaymentStatus.Unpaid
        && o.SaleId is not null
        && o.Status is OnlineOrderStatus.Ready or OnlineOrderStatus.AwaitingTransfer
            or OnlineOrderStatus.ReadyForPickup or OnlineOrderStatus.OnTheWay;

    private static void EnsureCanUpload(OnlineOrder o)
    {
        if (o.PaymentMethod != PaymentMethod.Transfer)
            throw new ValidationException("Bu buyurtma o'tkazma orqali to'lanmaydi.");
        if (o.PaymentStatus != OrderPaymentStatus.Unpaid)
            throw new ValidationException("To'lov allaqachon tasdiqlangan.");
        if (!ShowTransferCard(o))
            throw new ValidationException("Chekni buyurtma yig'ilgandan keyin yuklang (yakuniy summa shunda ma'lum bo'ladi).");
    }

    private static string FullName(ShopUser u)
    {
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return name.Length > 0 ? name : "Mijoz";
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
