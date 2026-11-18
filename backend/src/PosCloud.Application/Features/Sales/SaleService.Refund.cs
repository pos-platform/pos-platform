using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Application.Features.Stock;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Sales;

/// <summary>
/// Qaytarish (to'liq va qisman). Qoidalar (CONTRACT-10, 10.5):
/// <list type="bullet">
/// <item>Qatorlar ulushi telescoping usulida: share(q) = round(x·(R+q)/Q) − round(x·R/Q) — oxirgi qaytarishda qoldiq to'liq.</item>
/// <item>Keshbek: asl LoyaltyPointsEarned × (qaytarilgan tovar summasi / keshbek bazasi), kumulyativ; oxirgisida qoldiq to'liq.</item>
/// <item>Ball bilan to'langan qism shu nisbatda ballga qaytariladi (avval), keyin keshbek ayiriladi; balans yetmasa —
/// yetmagan qism puldan ushlab qolinadi (refund qatorlariga chegirma sifatida — asl redeem mexanizmi kabi).</item>
/// </list>
/// </summary>
public partial class SaleService
{
    public Task<SaleDto> RefundAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        RefundAsync(saleId, null, null, cancellationToken);

    public async Task<RefundPreviewDto> PreviewRefundAsync(Guid saleId, RefundRequest? request,
        CancellationToken cancellationToken = default)
    {
        var original = await LoadForRefundAsync(saleId, tracking: false, cancellationToken);
        var ctx = await LoadRefundContextAsync(original, cancellationToken);
        var plan = BuildRefundPlan(original, request, ctx);

        return new RefundPreviewDto
        {
            Items = plan.Lines.Select(l => new RefundPreviewItemDto
            {
                SaleItemId = l.Item.Id,
                ProductId = l.Item.ProductId,
                ProductName = l.Item.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.Item.UnitPrice,
                DiscountAmount = l.Discount,
                LineTotal = l.LineTotal
            }).ToList(),
            RefundTotal = plan.RefundTotal,
            PointsToReverse = plan.PointsToReverse,
            PointsAvailable = plan.PointsAvailable,
            WithheldAmount = plan.Withheld,
            CashToReturn = plan.RefundTotal - plan.Withheld,
            RedeemedPointsReturned = plan.RedeemReturned
        };
    }

    public async Task<SaleDto> RefundAsync(Guid saleId, RefundRequest? request, RefundOptions? options,
        CancellationToken cancellationToken = default)
    {
        options ??= new RefundOptions();
        var cashierId = _currentUser.UserId
            ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.Status == ShiftStatus.Open, cancellationToken);
        if (shift is null && !options.AllowWithoutShift)
            throw new ValidationException("Qaytarish uchun avval smenani oching.");

        var original = await LoadForRefundAsync(saleId, tracking: true, cancellationToken);
        var ctx = await LoadRefundContextAsync(original, cancellationToken);
        var plan = BuildRefundPlan(original, request, ctx);

        if (options.CashAsBonus && ctx.Customer is null)
            throw new ValidationException("Bonus bilan qaytarish uchun savdoda mijoz bo'lishi kerak.");

        // Ushlab qolingan summa refund qatorlariga chegirma sifatida taqsimlanadi (fiskal chekda asl ball bilan to'lov kabi).
        var withheldShares = OnlineOrderPricing.Distribute(plan.Lines.Select(l => l.LineTotal).ToList(), plan.Withheld);

        var refund = new Sale
        {
            BranchId = original.BranchId,
            CashierId = cashierId,
            // Bonus bilan qaytarishda kassadan pul chiqmaydi — smena hisobiga kirmaydi.
            CashShiftId = options.CashAsBonus ? null : shift?.Id,
            Type = SaleType.Refund,
            // Qaytarish qaysi kassada qabul qilingan bo'lsa — fiskal navbatda ham o'sha kassaga tushsin.
            TerminalId = ResolveTerminalId(null) ?? original.TerminalId,
            OriginalSaleId = original.Id,
            CustomerId = original.CustomerId,
            PaymentMethod = options.PaymentMethod ?? original.PaymentMethod,
            TransferSource = options.PaymentMethod is null or PaymentMethod.Transfer ? original.TransferSource : null,
            Status = SaleStatus.Completed,
            PaidAmount = 0,
            ChangeAmount = 0,
            RefundReason = string.IsNullOrWhiteSpace(request?.Reason) ? null : request!.Reason!.Trim(),
            ReceiptNumber = await GenerateReceiptNumberAsync("RR", cancellationToken)
        };

        for (var k = 0; k < plan.Lines.Count; k++)
        {
            var l = plan.Lines[k];
            var item = l.Item;
            var lineTotal = Round2(l.LineTotal - withheldShares[k]);
            refund.Items.Add(new SaleItem
            {
                OriginalSaleItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                MxikCode = item.MxikCode,
                Barcode = item.Barcode,
                MarkingCode = item.MarkingCode,
                PackageCode = item.PackageCode,
                CommitentTin = item.CommitentTin,
                Unit = item.Unit,
                Quantity = l.Quantity,
                UnitPrice = item.UnitPrice,
                CostPrice = item.CostPrice,
                DiscountAmount = Round2(l.Discount + withheldShares[k]),
                VatPercent = item.VatPercent,
                VatAmount = Round2(lineTotal * item.VatPercent / (100m + item.VatPercent)),
                LineTotal = lineTotal
            });
            item.RefundedQuantity += l.Quantity;
        }

        refund.ServiceChargePercent = plan.ServiceCharge > 0 ? original.ServiceChargePercent : 0;
        refund.ServiceChargeAmount = plan.ServiceCharge;
        refund.TotalAmount = Round2(refund.Items.Sum(i => i.LineTotal) + plan.ServiceCharge);
        refund.DiscountAmount = refund.Items.Sum(i => i.DiscountAmount);
        refund.VatAmount = refund.Items.Sum(i => i.VatAmount);
        // Aralash to'lov: karta qismi asl nisbatda (to'liq qaytarishda — asl karta qismi).
        refund.CardAmount = refund.PaymentMethod == PaymentMethod.Mixed && original.TotalAmount > 0
            ? Math.Min(refund.TotalAmount, Round2(original.CardAmount * refund.TotalAmount / original.TotalAmount))
            : 0;
        refund.LoyaltyWithheldAmount = plan.Withheld;
        refund.RedeemedPointsReturned = plan.RedeemReturned;
        refund.RefundCashAmount = options.CashAsBonus ? 0 : refund.TotalAmount;
        refund.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        original.Status = plan.FullyRefundedAfter ? SaleStatus.Refunded : SaleStatus.PartiallyRefunded;

        // Bonus: avval ball bilan to'langan qism qaytariladi, keyin keshbek ayiriladi — sof natija to'g'ri chiqadi.
        if (ctx.Customer is { } customer)
        {
            var source = LoyaltyRules.SourceFor(original.ClientSaleId);
            var userName = _currentUser.Username;
            var suffix = string.IsNullOrWhiteSpace(options.Note) ? string.Empty : $" ({options.Note})";
            if (plan.RedeemReturned > 0)
                LoyaltyRules.Apply(_db, customer, plan.RedeemReturned, LoyaltyTransactionType.Refund, source, plan.RedeemReturned,
                    $"Qaytarish {refund.ReceiptNumber} — ball bilan to'langan qism qaytarildi{suffix}", refund,
                    userId: cashierId, userName: userName);
            if (plan.Reversed > 0)
            {
                var withheldNote = plan.Withheld > 0 ? $", {plan.Withheld:0} so'm puldan ushlab qolindi" : string.Empty;
                refund.LoyaltyPointsReversed = -LoyaltyRules.Apply(_db, customer, -plan.Reversed, LoyaltyTransactionType.Refund,
                    source, plan.RefundTotal, $"Qaytarish {refund.ReceiptNumber} — keshbek qaytarib olindi{withheldNote}{suffix}",
                    refund, userId: cashierId, userName: userName);
            }
            if (options.CashAsBonus && refund.TotalAmount > 0)
                LoyaltyRules.Apply(_db, customer, Math.Floor(refund.TotalAmount), LoyaltyTransactionType.Refund, source,
                    refund.TotalAmount, $"Qaytarish {refund.ReceiptNumber} — pul o'rniga bonus{suffix}", refund,
                    userId: cashierId, userName: userName);
        }

        // Qaytarilgan tovarlar filial qoldig'iga qaytadi (faqat qaytarilgan miqdor, faqat ombor tovari).
        if (options.ReturnStock && original.BranchId is { } branchId)
            await ReturnToStockAsync(branchId,
                plan.Lines.Where(l => ctx.TypeOf(l.Item.ProductId) is not (ProductType.Service or ProductType.Dish))
                    .Select(l => (l.Item.ProductId, l.Quantity)).ToList(),
                StockMovementType.Refund, refund.Id, refund.ReceiptNumber,
                cancellationToken);

        _db.Sales.Add(refund);
        await _db.SaveChangesAsync(cancellationToken);

        // Return cheki fiskal navbatga — faqat asl savdo fiskal (QR olgan) bo'lsa.
        var originalWasFiscal = !string.IsNullOrWhiteSpace(original.Receipt?.QrCodeUrl);
        await EnqueueFiscalIfNeededAsync(refund, originalWasFiscal, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(refund);
    }

    /// <summary>
    /// Tovarni filial qoldig'iga qaytaradi va eng yangi FIFO qatlamiga qo'shadi ("qatlamlar jami = qoldiq" invarianti).
    /// SaveChanges chaqirmaydi. Online "rejected-received" ham shundan foydalanadi.
    /// </summary>
    public async Task ReturnToStockAsync(Guid branchId, IReadOnlyCollection<(Guid ProductId, decimal Quantity)> lines,
        StockMovementType movementType, Guid? documentId, string? documentNumber,
        CancellationToken cancellationToken = default)
    {
        var perProduct = lines.Where(l => l.Quantity > 0)
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        if (perProduct.Count == 0) return;

        var ids = perProduct.Keys.ToList();
        var stocks = await _db.BranchStocks
            .Where(s => s.BranchId == branchId && ids.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);
        var layers = await _db.StockReceiptItems
            .Where(li => ids.Contains(li.ProductId) && li.StockReceipt!.BranchId == branchId)
            .ToListAsync(cancellationToken);
        var newest = layers.GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(li => li.CreatedAt).First());

        var doc = new StockDoc(documentId, documentNumber, _currentUser.UserId);
        foreach (var (productId, qty) in perProduct)
        {
            var stock = StockLedger.GetOrCreate(_db, stocks, branchId, productId);
            newest.TryGetValue(productId, out var layer);
            StockLedger.Apply(_db, stock, qty, movementType, doc, layer?.CostPrice);
            if (layer is not null)
                layer.RemainingQuantity += qty;
        }
    }

    // ---------------- qaytarish rejasi ----------------

    private async Task<Sale> LoadForRefundAsync(Guid saleId, bool tracking, CancellationToken ct)
    {
        IQueryable<Sale> query = _db.Sales.Include(s => s.Items).Include(s => s.Receipt);
        if (!tracking) query = query.AsNoTracking();
        var original = await query.FirstOrDefaultAsync(s => s.Id == saleId, ct)
                       ?? throw new NotFoundException($"Savdo topilmadi: {saleId}");

        if (original.Type != SaleType.Sale)
            throw new ValidationException("Faqat oddiy savdoni qaytarish mumkin.");
        if (original.Status == SaleStatus.Refunded)
            throw new ConflictException("Bu savdo allaqachon qaytarilgan.");
        if (original.Status is SaleStatus.Pending or SaleStatus.Cancelled)
            throw new ValidationException("Faqat yakunlangan savdoni qaytarish mumkin.");
        return original;
    }

    private sealed record RefundContext(
        Customer? Customer,
        IReadOnlyDictionary<Guid, ProductType> Types,
        decimal PriorReversed,
        decimal PriorRedeemReturned,
        decimal RedeemBase,
        IReadOnlyDictionary<Guid, decimal> PreEarnRefunded)
    {
        public ProductType? TypeOf(Guid productId) => Types.TryGetValue(productId, out var t) ? t : null;
    }

    private async Task<RefundContext> LoadRefundContextAsync(Sale original, CancellationToken ct)
    {
        Customer? customer = original.CustomerId is { } cid
            ? await _db.Customers.FirstOrDefaultAsync(c => c.Id == cid, ct)
            : null;

        var productIds = original.Items.Select(i => i.ProductId).Distinct().ToList();
        var types = await _db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.ProductType, ct);

        var prior = await _db.Sales.AsNoTracking()
            .Where(s => s.OriginalSaleId == original.Id && s.Type == SaleType.Refund)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Reversed = g.Sum(s => s.LoyaltyPointsReversed + s.LoyaltyWithheldAmount),
                Redeem = g.Sum(s => s.RedeemedPointsReturned)
            })
            .FirstOrDefaultAsync(ct);

        // Ball bilan to'langan qism: kassada — Sale.LoyaltyPointsRedeemed; online buyurtmada — OnlineOrder.BonusUsed
        // (faqat yetkazilgan buyurtma; yetkazilmasdan bekor qilinganda bonusni OnlineOrderService o'zi qaytaradi).
        var redeemBase = original.LoyaltyPointsRedeemed;
        var preEarn = new Dictionary<Guid, decimal>();
        if (LoyaltyRules.SourceFor(original.ClientSaleId) == LoyaltySource.Online)
        {
            // Kuzatuvdagi (tracked) nusxa — chaqiruvchi servis holatni xotirada o'zgartirgan bo'lsa (masalan, kuryer
            // "Yetkazildi" + eshik oldida rad etish) shu qiymat ko'rinadi.
            var order = await _db.OnlineOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.SaleId == original.Id, ct);
            if (redeemBase <= 0 && order is { Status: OnlineOrderStatus.Delivered, BonusCharged: true })
                redeemBase = order.BonusUsed;
            // Eshik oldida rad etilgan miqdorlar keshbek hisoblanishidan OLDIN qaytarilgan — keshbek bazasiga kirmaydi.
            if (order is not null)
                foreach (var i in order.Items.Where(i => i.RejectedQuantity > 0))
                    preEarn[i.ProductId] = preEarn.GetValueOrDefault(i.ProductId) + i.RejectedQuantity;
        }

        return new RefundContext(customer, types, prior?.Reversed ?? 0, prior?.Redeem ?? 0, redeemBase, preEarn);
    }

    private sealed record RefundLine(SaleItem Item, decimal Quantity, decimal LineTotal, decimal Discount);

    private sealed class RefundPlan
    {
        public List<RefundLine> Lines { get; } = [];
        public decimal RefundTotal { get; set; }
        public decimal ServiceCharge { get; set; }
        public bool FullyRefundedAfter { get; set; }
        public decimal PointsToReverse { get; set; }
        public decimal PointsAvailable { get; set; }
        public decimal Reversed { get; set; }
        public decimal Withheld { get; set; }
        public decimal RedeemReturned { get; set; }
    }

    private RefundPlan BuildRefundPlan(Sale original, RefundRequest? request, RefundContext ctx)
    {
        // So'ralgan miqdorlar (body yo'q yoki bo'sh — qolgan hamma miqdor).
        var requested = new Dictionary<Guid, decimal>();
        if (request?.Items is { Count: > 0 } items)
        {
            foreach (var r in items)
            {
                var item = original.Items.FirstOrDefault(i => i.Id == r.SaleItemId)
                           ?? throw new ValidationException("Qaytariladigan qator bu chekka tegishli emas.");
                if (r.Quantity <= 0 || decimal.Round(r.Quantity, 3) != r.Quantity)
                    throw new ValidationException($"'{item.ProductName}' uchun qaytarish miqdori noto'g'ri.");
                requested[item.Id] = requested.GetValueOrDefault(item.Id) + r.Quantity;
            }
            foreach (var (id, qty) in requested)
            {
                var item = original.Items.First(i => i.Id == id);
                var left = item.Quantity - item.RefundedQuantity;
                if (qty > left)
                    throw new ValidationException(
                        $"'{item.ProductName}' dan ko'pi bilan {left:0.###} qaytarish mumkin.");
            }
        }
        else
        {
            foreach (var item in original.Items.Where(i => i.Quantity - i.RefundedQuantity > 0))
                requested[item.Id] = item.Quantity - item.RefundedQuantity;
        }

        if (requested.Count == 0)
            throw new ConflictException("Bu savdo allaqachon qaytarilgan.");

        var plan = new RefundPlan();
        foreach (var item in original.Items.OrderBy(i => i.CreatedAt))
        {
            if (!requested.TryGetValue(item.Id, out var qty)) continue;
            plan.Lines.Add(new RefundLine(item, qty,
                Share(item.LineTotal, item.Quantity, item.RefundedQuantity, qty),
                Share(item.DiscountAmount, item.Quantity, item.RefundedQuantity, qty)));
        }

        decimal After(SaleItem i) => i.RefundedQuantity + requested.GetValueOrDefault(i.Id);
        plan.FullyRefundedAfter = original.Items.All(i => After(i) >= i.Quantity);
        // Servis haqi (restoran) qatorlarda emas — oxirgi (to'liq) qaytarishda qo'shiladi.
        plan.ServiceCharge = plan.FullyRefundedAfter ? original.ServiceChargeAmount : 0;
        plan.RefundTotal = Round2(plan.Lines.Sum(l => l.LineTotal) + plan.ServiceCharge);

        // Keshbek bazasi — tovar qatorlari (xizmat, masalan yetkazish, hisobga olinmaydi).
        var goods = original.Items.Where(i => ctx.TypeOf(i.ProductId) != ProductType.Service).ToList();
        // Keshbekdan oldin qaytarilgan (eshik oldida rad etilgan) miqdor har qatordan chiqariladi — aks holda
        // keyingi qaytarishlarda ayiriladigan keshbek kam chiqadi (keshbek faqat olingan tovarlarga berilgan).
        var preLeft = new Dictionary<Guid, decimal>(ctx.PreEarnRefunded);
        var pre = new Dictionary<Guid, decimal>();
        foreach (var i in goods)
        {
            var p = Math.Min(i.Quantity, preLeft.GetValueOrDefault(i.ProductId));
            if (p <= 0) continue;
            pre[i.Id] = p;
            preLeft[i.ProductId] -= p;
        }
        decimal Pre(SaleItem i) => pre.GetValueOrDefault(i.Id);
        var baseTotal = goods.Sum(i => Round2(i.Quantity > 0 ? i.LineTotal * (i.Quantity - Pre(i)) / i.Quantity : 0));
        var cumAfter = goods.Sum(i => Round2(i.Quantity > 0 ? i.LineTotal * Math.Max(0, After(i) - Pre(i)) / i.Quantity : 0));
        var goodsFinal = goods.Count > 0 ? goods.All(i => After(i) >= i.Quantity) : plan.FullyRefundedAfter;

        decimal Target(decimal total) => total <= 0 ? 0
            : goodsFinal ? total
            : baseTotal > 0 ? Math.Floor(total * Math.Min(1, cumAfter / baseTotal)) : 0;

        if (ctx.Customer is not null)
        {
            plan.RedeemReturned = Math.Max(0, Target(ctx.RedeemBase) - ctx.PriorRedeemReturned);
            plan.PointsToReverse = Math.Max(0, Target(original.LoyaltyPointsEarned) - ctx.PriorReversed);
            plan.PointsAvailable = Math.Floor(Math.Max(0, ctx.Customer.LoyaltyPoints)) + plan.RedeemReturned;
            plan.Reversed = Math.Min(plan.PointsToReverse, plan.PointsAvailable);
            plan.Withheld = Math.Min(plan.PointsToReverse - plan.Reversed,
                Math.Floor(Math.Max(0, plan.Lines.Sum(l => l.LineTotal))));
        }

        return plan;
    }

    /// <summary>Qator summasining q miqdorga to'g'ri keladigan ulushi (oxirgi qaytarishda qoldiq to'liq).</summary>
    private static decimal Share(decimal total, decimal quantity, decimal refundedBefore, decimal qty)
    {
        if (quantity <= 0) return 0;
        var before = Round2(total * refundedBefore / quantity);
        var after = refundedBefore + qty >= quantity ? total : Round2(total * (refundedBefore + qty) / quantity);
        return after - before;
    }
}
