using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Sales;

public class SaleService : ISaleService
{
    /// <summary>O'zbekiston standart QQS stavkasi (%).</summary>
    private const decimal DefaultVatPercent = 12m;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFiscalReceiptService _fiscal;

    public SaleService(IApplicationDbContext db, ICurrentUser currentUser, IFiscalReceiptService fiscal)
    {
        _db = db;
        _currentUser = currentUser;
        _fiscal = fiscal;
    }

    /// <summary>
    /// Savdo yoziladigan kassa ID sini aniqlaydi. Login biror kassaga biriktirilgan bo'lsa —
    /// savdo faqat o'sha kassa nomi bilan yoziladi; kassa ilovasida boshqa ID turgan bo'lsa
    /// savdo rad etiladi (aks holda bitta login ostidan bir nechta kassaning fiskal
    /// navbati va QR summalari aralashib ketadi).
    /// </summary>
    private string? ResolveTerminalId(string? requestedTerminalId)
    {
        var requested = TerminalIdentity.Normalize(requestedTerminalId);
        var assigned = TerminalIdentity.Normalize(_currentUser.TerminalId);
        if (assigned is null) return requested;

        if (requested is not null && !TerminalIdentity.Matches(requested, assigned))
            throw new ValidationException(
                $"Bu login '{assigned}' kassasiga biriktirilgan, savdo esa '{requested}' kassasidan kelyapti. " +
                "Kassa ilovasi Sozlamalaridagi \"Kassa stantsiyasi ID\" ni to'g'rilang.");

        return assigned;
    }

    public async Task<IReadOnlyList<SaleDto>> GetAllAsync(Guid? branchId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
            .Include(s => s.Cashier) // kassir bo'yicha hisobot uchun (kim sotgani)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId);

        // Sana chegaralari UTC kalendar kuni sifatida (ReportService bilan bir xil konvensiya).
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt < toUtc);
        }

        var sales = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return sales.Select(SaleDto.FromEntity).ToList();
    }

    public async Task<SaleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale is null)
        {
            throw new NotFoundException($"Savdo topilmadi: {id}");
        }

        return SaleDto.FromEntity(sale);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var cashierId = _currentUser.UserId
            ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

        // Idempotentlik: kassa javobni ololmay qayta yuborgan bo'lsa (tarmoq uzildi yoki
        // oflayn navbat qayta urinmoqda), yangi savdo yaratmaymiz — avvalgisini qaytaramiz.
        var clientSaleId = string.IsNullOrWhiteSpace(request.ClientSaleId)
            ? null
            : request.ClientSaleId.Trim();
        if (clientSaleId is not null)
        {
            var duplicate = await FindByClientSaleIdAsync(clientSaleId, cancellationToken);
            if (duplicate is not null) return duplicate;
        }

        if (request.Items.Count == 0)
        {
            throw new ValidationException("Savdoda kamida bitta mahsulot bo'lishi kerak.");
        }

        // Savdo qilish uchun ochiq smena bo'lishi shart.
        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(
                s => s.CashierId == cashierId && s.Status == ShiftStatus.Open,
                cancellationToken)
            ?? throw new ValidationException("Avval smenani oching (kassa sessiyasi).");

        // Nasiya (Debt) bo'lsa — qarzdor majburiy.
        Debtor? debtor = null;
        if (request.PaymentMethod == PaymentMethod.Debt)
        {
            if (request.DebtorId is null)
                throw new ValidationException("Nasiya uchun qarzdorni tanlang.");
            debtor = await _db.Debtors.FirstOrDefaultAsync(d => d.Id == request.DebtorId, cancellationToken)
                ?? throw new NotFoundException("Qarzdor topilmadi.");
        }

        // Mahsulotlarni bitta so'rovda yuklaymiz.
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var saleBranchId = request.BranchId ?? _currentUser.BranchId
            ?? throw new ValidationException("Savdo uchun filial aniqlanmadi.");

        var branchStocks = await _db.BranchStocks
            .Where(s => s.BranchId == saleBranchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        // FIFO tannarx qatlamlari — shu filialning qoldig'i bor kirim partiyalari, eng eski birinchi.
        var layersByProduct = await LoadCostLayersAsync(saleBranchId, productIds, cancellationToken);

        // Amaldagi aksiyalar — chegirma server tomonida hisoblanadi (mijozga ishonmaymiz).
        var nowUtc = DateTime.UtcNow;
        var activePromotions = await _db.Promotions
            .Where(p => p.IsActive && p.StartDate <= nowUtc && p.EndDate >= nowUtc)
            .ToListAsync(cancellationToken);

        // Mijoz (sodiqlik dasturi, ixtiyoriy).
        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        }

        var sale = new Sale
        {
            ClientSaleId = clientSaleId,
            BranchId = request.BranchId ?? _currentUser.BranchId,
            CashierId = cashierId,
            CashShiftId = shift.Id,
            TerminalId = ResolveTerminalId(request.TerminalId),
            Type = SaleType.Sale,
            PaymentMethod = request.PaymentMethod,
            TransferSource = request.PaymentMethod == PaymentMethod.Transfer && !string.IsNullOrWhiteSpace(request.TransferSource)
                ? request.TransferSource.Trim()
                : null,
            DebtorId = debtor?.Id,
            CustomerId = customer?.Id,
            Status = SaleStatus.Completed
        };

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            }

            if (line.Quantity <= 0)
            {
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");
            }

            // Markirovkali tovar — har bir jismoniy dona uchun DataMatrix kodi majburiy
            // (fiskal chekning Labels maydoniga uzatilib, Asl Belgisi'da oborotdan chiqadi).
            // Bunday qator faqat bitta donani bildiradi (kod noyob), miqdori 1 bo'lishi shart.
            var markingCode = string.IsNullOrWhiteSpace(line.MarkingCode) ? null : line.MarkingCode.Trim();
            if (product.IsMarked)
            {
                if (markingCode is null)
                    throw new ValidationException($"'{product.Name}' markirovkali tovar — DataMatrix kodi skanerlanishi shart.");
                if (line.Quantity != 1)
                    throw new ValidationException($"'{product.Name}' markirovkali tovar — har bir dona alohida skanerlanadi (miqdor 1 bo'lishi kerak).");
            }

            var unitPrice = line.UnitPriceOverride ?? product.Price;
            // QQS stavkasi mahsulot kartochkasidan olinadi — kassa yuborgan stavkaga ishonmaymiz.
            var vatPercent = VatPercentFor(product);

            var gross = Round2(unitPrice * line.Quantity);

            // Aksiya chegirmasi (avtomatik) + mijoz yuborgan qo'lda chegirma (agar bo'lsa).
            var promoPercent = PromotionCalculator.GetDiscountPercent(
                activePromotions, product.Id, product.CategoryId);
            var promoDiscount = Round2(gross * promoPercent / 100m);
            var discount = Round2(promoDiscount + line.DiscountAmount);

            var lineTotal = Round2(gross - discount);
            if (lineTotal < 0)
            {
                throw new ValidationException($"'{product.Name}' uchun chegirma qator summasidan katta.");
            }

            // O'zbekistonda chakana narx QQS'ni o'z ichiga oladi.
            var vatAmount = Round2(lineTotal * vatPercent / (100m + vatPercent));

            // Tannarx: tovar (Good) uchun FIFO qatlamlardan (eski/arzon partiya birinchi
            // sotiladi → eski tovar foydasi yuqori chiqadi); ovqat (Dish) uchun joriy tannarx.
            // Ombordan ayirish ham faqat Good uchun; xom-ashyo KitchenIssue orqali kamayadi.
            decimal costPrice;
            if (product.ProductType == ProductType.Good)
            {
                if (!branchStocks.TryGetValue(product.Id, out var stock) || stock.Quantity < line.Quantity)
                    throw new ValidationException($"'{product.Name}' qoldig'i yetarli emas (mavjud: {stock?.Quantity ?? 0}).");
                stock.Quantity -= line.Quantity;

                layersByProduct.TryGetValue(product.Id, out var prodLayers);
                costPrice = ConsumeFifoCost(prodLayers, line.Quantity, product.CostPrice);
            }
            else
            {
                costPrice = product.CostPrice;
            }

            sale.Items.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                MxikCode = product.MxikCode,
                Barcode = product.Barcode,
                MarkingCode = markingCode,
                PackageCode = product.PackageCode,
                CommitentTin = product.CommitentTin,
                Unit = product.Unit,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                CostPrice = costPrice,
                DiscountAmount = discount,
                VatPercent = vatPercent,
                VatAmount = vatAmount,
                LineTotal = lineTotal
            });
        }

        sale.TotalAmount = sale.Items.Sum(i => i.LineTotal);
        sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
        sale.VatAmount = sale.Items.Sum(i => i.VatAmount);

        // Kassada tiyin ishlatilmaydi — tarozili mahsulotlar tufayli jami kasrli bo'lsa
        // (masalan 223 070,4), butun so'mga yaxlitlangan qiymat bilan solishtiramiz.
        if (request.PaymentMethod == PaymentMethod.Cash && request.PaidAmount < Math.Round(sale.TotalAmount, 0, MidpointRounding.AwayFromZero))
        {
            throw new ValidationException("Berilgan summa savdo summasidan kam.");
        }

        if (request.PaymentMethod == PaymentMethod.Debt)
        {
            // Nasiya — to'lov yo'q, hammasi qarzga yoziladi.
            sale.PaidAmount = 0;
            sale.ChangeAmount = 0;
        }
        else
        {
            sale.PaidAmount = request.PaidAmount;
            sale.ChangeAmount = Math.Max(0, Round2(request.PaidAmount - sale.TotalAmount));
        }
        ApplyCardAmount(sale, request.PaymentMethod, request.CardAmount);

        sale.ReceiptNumber = await GenerateReceiptNumberAsync("R", cancellationToken);

        sale.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        _db.Sales.Add(sale);

        // Mijoz sodiqlik ballari (savdo summasining 1%).
        if (customer is not null)
        {
            var points = Round2(sale.TotalAmount * 0.01m);
            customer.LoyaltyPoints += points;
            sale.LoyaltyPointsEarned = points;
            _db.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CustomerId = customer.Id,
                Points = points,
                Amount = sale.TotalAmount,
                Comment = $"Savdo {sale.ReceiptNumber}",
                Sale = sale
            });
        }

        // Nasiya — qarzdor balansiga qo'shamiz va harakat yozamiz.
        if (debtor is not null)
        {
            debtor.Balance += sale.TotalAmount;
            _db.DebtorTransactions.Add(new DebtorTransaction
            {
                DebtorId = debtor.Id,
                Type = DebtTransactionType.Debt,
                Amount = sale.TotalAmount,
                BalanceAfter = debtor.Balance,
                Comment = $"Savdo {sale.ReceiptNumber}"
            });
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (clientSaleId is not null)
        {
            // Xuddi shu kalitli so'rov ayni paytda parallel kelgan bo'lishi mumkin (kassa
            // birinchi so'rovning javobini kutmasdan qayta yuborgan). Unique indeks ikkinchisini
            // rad etadi — tranzaksiya bekor bo'ladi, biz esa birinchi savdoni qaytaramiz.
            var duplicate = await FindByClientSaleIdAsync(clientSaleId, cancellationToken);
            if (duplicate is not null) return duplicate;
            throw; // boshqa sabab — asl xatoni yashirmaymiz
        }

        // Fiskalizatsiya (nasiyadan tashqari — pastdagi izohga qarang). Kassir "Chop etish"
        // (nofiskal, QR-siz) tugmasini bosgan bo'lsa request.Fiscalize=false keladi — chek
        // soliqqa umuman yuborilmaydi (QrCodeUrl bo'sh qoladi → hisobotda "QR kodsiz").
        await EnqueueFiscalIfNeededAsync(sale, request.Fiscalize, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(sale);
    }

    /// <summary>Idempotentlik kaliti bo'yicha avval yaratilgan savdoni topadi (bo'lmasa null).</summary>
    private async Task<SaleDto?> FindByClientSaleIdAsync(string clientSaleId, CancellationToken cancellationToken)
    {
        var existing = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
            .FirstOrDefaultAsync(s => s.ClientSaleId == clientSaleId, cancellationToken);
        return existing is null ? null : SaleDto.FromEntity(existing);
    }

    /// <summary>
    /// Chekni fiskal navbatga qo'yadi. Fiskalizatsiya QILINMAYDI, agar:
    /// <list type="bullet">
    /// <item><b>Nasiya (Debt)</b> — tovar berilgan, lekin pul olinmagan; chekni hozir ro'yxatga
    /// olsak modulga naqd tushum sifatida tushadi va kassadagi haqiqiy naqd bilan mos kelmaydi.
    /// Qarz to'langanda fiskal chek chiqarilishi kerak (hozircha qo'lda).</item>
    /// <item><paramref name="fiscalize"/> = false — kassir "Chop etish" (nofiskal, QR-siz)
    /// tugmasini bosgan; chek ataylab soliqqa yuborilmaydi.</item>
    /// </list>
    /// </summary>
    private async Task EnqueueFiscalIfNeededAsync(Sale sale, bool fiscalize, CancellationToken cancellationToken)
    {
        if (!fiscalize)
            return;

        if (sale.PaymentMethod == PaymentMethod.Debt)
            return;

        // MXIK'siz yoki noto'g'ri MXIK bilan chek soliqda tekshiruvdan o'tmaydi — modulga
        // yubormaymiz, Failed qilib sababini yozamiz (fiskal audit sahifasida ko'rinadi).
        var badMxik = sale.Items.Where(i => !Mxik.IsValid(i.MxikCode)).Select(i => i.ProductName).ToList();
        if (badMxik.Count > 0)
        {
            if (sale.Receipt is not null)
            {
                sale.Receipt.FiscalStatus = FiscalStatus.Failed;
                sale.Receipt.ErrorMessage =
                    $"MXIK (ИКПУ) kodi {Mxik.Length} xonali emas: {string.Join(", ", badMxik)}. " +
                    "Mahsulot kartochkasida MXIK'ni to'g'rilang.";
            }
            return;
        }

        await _fiscal.EnqueueAsync(sale, cancellationToken);
    }

    /// <summary>QQS stavkasi: QQS to'lovchi tovar — 12%, "QQSsiz" (IsVat=false) — 0%.</summary>
    private static decimal VatPercentFor(Product product) => product.IsVat ? DefaultVatPercent : 0m;

    public async Task<SaleDto> RefundAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var cashierId = _currentUser.UserId
            ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(
                s => s.CashierId == cashierId && s.Status == ShiftStatus.Open,
                cancellationToken)
            ?? throw new ValidationException("Qaytarish uchun avval smenani oching.");

        var original = await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Receipt) // qaytarish asl chekning fiskal holatiga mos bo'lishi uchun
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (original is null)
        {
            throw new NotFoundException($"Savdo topilmadi: {saleId}");
        }
        if (original.Type != SaleType.Sale)
        {
            throw new ValidationException("Faqat oddiy savdoni qaytarish mumkin.");
        }
        if (original.Status == SaleStatus.Refunded)
        {
            throw new ConflictException("Bu savdo allaqachon qaytarilgan.");
        }

        var refund = new Sale
        {
            BranchId = original.BranchId,
            CashierId = cashierId,
            CashShiftId = shift.Id,
            Type = SaleType.Refund,
            // Qaytarish qaysi kassada qabul qilingan bo'lsa — fiskal navbatda ham o'sha kassaga
            // tushsin (agent terminal bo'yicha filtrlaydi). Kassa biriktirilmagan bo'lsa —
            // asl savdoning kassasiga qaytamiz.
            TerminalId = ResolveTerminalId(null) ?? original.TerminalId,
            OriginalSaleId = original.Id,
            PaymentMethod = original.PaymentMethod,
            TransferSource = original.TransferSource,
            Status = SaleStatus.Completed,
            TotalAmount = original.TotalAmount,
            DiscountAmount = original.DiscountAmount,
            VatAmount = original.VatAmount,
            PaidAmount = 0,
            ChangeAmount = 0,
            // Qaytarish asl to'lov shakli bo'yicha: naqd va karta qismlari asl chekdagidek.
            CardAmount = original.CardAmount,
            ReceiptNumber = await GenerateReceiptNumberAsync("RR", cancellationToken)
        };

        foreach (var item in original.Items)
        {
            refund.Items.Add(new SaleItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                MxikCode = item.MxikCode,
                Barcode = item.Barcode,
                MarkingCode = item.MarkingCode,
                PackageCode = item.PackageCode,
                CommitentTin = item.CommitentTin,
                Unit = item.Unit,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CostPrice = item.CostPrice,
                DiscountAmount = item.DiscountAmount,
                VatPercent = item.VatPercent,
                VatAmount = item.VatAmount,
                LineTotal = item.LineTotal
            });
        }

        refund.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        original.Status = SaleStatus.Refunded;

        // Qaytarilgan tovarlar filial qoldig'iga qaytadi.
        var refundBranchId = original.BranchId;
        if (refundBranchId.HasValue)
        {
            var refundProductIds = original.Items.Select(i => i.ProductId).Distinct().ToList();
            var refundStocks = await _db.BranchStocks
                .Where(s => s.BranchId == refundBranchId && refundProductIds.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, cancellationToken);

            // Qaytarilgan miqdorni eng yangi FIFO qatlamiga qaytaramiz —
            // "qatlamlar jami = qoldiq" invariantini saqlash uchun.
            var refundLayers = await _db.StockReceiptItems
                .Where(li => refundProductIds.Contains(li.ProductId) && li.StockReceipt!.BranchId == refundBranchId)
                .ToListAsync(cancellationToken);
            var newestLayerByProduct = refundLayers
                .GroupBy(li => li.ProductId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(li => li.CreatedAt).First());

            foreach (var item in original.Items)
            {
                if (!refundStocks.TryGetValue(item.ProductId, out var stock))
                {
                    stock = new Domain.Entities.BranchStock { BranchId = refundBranchId.Value, ProductId = item.ProductId };
                    _db.BranchStocks.Add(stock);
                    refundStocks[item.ProductId] = stock;
                }
                stock.Quantity += item.Quantity;

                if (newestLayerByProduct.TryGetValue(item.ProductId, out var layer))
                    layer.RemainingQuantity += item.Quantity;
            }
        }

        _db.Sales.Add(refund);
        await _db.SaveChangesAsync(cancellationToken);

        // Return cheki ham fiskalizatsiya navbatiga — LEKIN faqat asl savdo fiskal (QR olgan)
        // bo'lsa. Nofiskal (QR-siz) yoki nasiya savdosining qaytarishi ham nofiskal bo'ladi,
        // aks holda soliqda mos kelmaydigan minus chek paydo bo'lardi.
        var originalWasFiscal = !string.IsNullOrWhiteSpace(original.Receipt?.QrCodeUrl);
        await EnqueueFiscalIfNeededAsync(refund, originalWasFiscal, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(refund);
    }

    // ===================== Restoran: ochiq buyurtma =====================

    public async Task<SaleDto> OpenOrderAsync(OpenOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken)
            ?? throw new NotFoundException("Stol topilmadi.");

        var existing = await _db.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.TableId == request.TableId && s.Status == SaleStatus.Pending, cancellationToken);
        if (existing is not null)
            return SaleDto.FromEntity(existing); // stolda allaqachon ochiq buyurtma bor

        var sale = new Sale
        {
            BranchId = request.BranchId ?? table.BranchId ?? _currentUser.BranchId,
            CashierId = userId,
            WaiterId = userId,
            TableId = table.Id,
            GuestCount = request.GuestCount,
            Type = SaleType.Sale,
            Status = SaleStatus.Pending,
            OpenedAt = DateTime.UtcNow,
            ReceiptNumber = await GenerateReceiptNumberAsync("O", cancellationToken)
        };

        table.Status = TableStatus.Occupied;

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(cancellationToken);
        return SaleDto.FromEntity(sale);
    }

    public async Task<IReadOnlyList<SaleDto>> GetOpenOrdersAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Where(s => s.Status == SaleStatus.Pending);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId);

        var orders = await query.OrderBy(s => s.OpenedAt).ToListAsync(cancellationToken);
        return orders.Select(SaleDto.FromEntity).ToList();
    }

    public async Task<SaleDto?> GetOpenOrderByTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var order = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.TableId == tableId && s.Status == SaleStatus.Pending, cancellationToken);
        return order is null ? null : SaleDto.FromEntity(order);
    }

    public async Task<SaleDto> SaveOrderItemsAsync(Guid saleId, SaveOrderItemsRequest request, CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken)
            ?? throw new NotFoundException($"Buyurtma topilmadi: {saleId}");
        if (sale.Status != SaleStatus.Pending)
            throw new ValidationException("Faqat ochiq buyurtmani tahrirlash mumkin.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var activePromotions = await _db.Promotions
            .Where(p => p.IsActive && p.StartDate <= nowUtc && p.EndDate >= nowUtc)
            .ToListAsync(cancellationToken);

        var existingItems = sale.Items.ToList();
        var matched = new HashSet<SaleItem>();
        var finalItems = new List<SaleItem>();

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");
            if (line.Quantity <= 0)
                throw new ValidationException($"'{product.Name}' uchun miqdor 0 dan katta bo'lishi kerak.");

            var note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim();
            var unitPrice = line.UnitPriceOverride ?? product.Price;
            var vatPercent = VatPercentFor(product);
            var gross = Round2(unitPrice * line.Quantity);
            var promoPercent = PromotionCalculator.GetDiscountPercent(activePromotions, product.Id, product.CategoryId);
            var discount = Round2(gross * promoPercent / 100m);
            var lineTotal = Round2(gross - discount);
            var vatAmount = Round2(lineTotal * vatPercent / (100m + vatPercent));

            // Bir xil mahsulot+izoh bo'yicha mavjud qatorni topib yangilaymiz (SentToKitchen saqlanadi).
            var item = existingItems.FirstOrDefault(e => !matched.Contains(e) && e.ProductId == line.ProductId && (e.Note ?? "") == (note ?? ""));
            if (item is null)
            {
                item = new SaleItem { SaleId = sale.Id, ProductId = product.Id, SentToKitchen = false };
                _db.SaleItems.Add(item);
            }
            else
            {
                matched.Add(item);
            }

            item.ProductName = product.Name;
            item.MxikCode = product.MxikCode;
            item.Barcode = product.Barcode;
            item.PackageCode = product.PackageCode;
            item.CommitentTin = product.CommitentTin;
            item.Unit = product.Unit;
            item.Quantity = line.Quantity;
            item.UnitPrice = unitPrice;
            item.CostPrice = product.CostPrice;
            item.DiscountAmount = discount;
            item.VatPercent = vatPercent;
            item.VatAmount = vatAmount;
            item.LineTotal = lineTotal;
            item.Note = note;
            finalItems.Add(item);
        }

        // Yangi ro'yxatda yo'q eski qatorlarni o'chiramiz.
        foreach (var leftover in existingItems.Where(e => !matched.Contains(e)).ToList())
            _db.SaleItems.Remove(leftover);

        // Jami — yakuniy qatorlardan (navigatsiyaga tayanmaymiz).
        sale.TotalAmount = finalItems.Sum(i => i.LineTotal);
        sale.DiscountAmount = finalItems.Sum(i => i.DiscountAmount);
        sale.VatAmount = finalItems.Sum(i => i.VatAmount);

        await _db.SaveChangesAsync(cancellationToken);

        // Yangilangan qatorlar bilan to'liq o'qib qaytaramiz.
        return await GetByIdAsync(saleId, cancellationToken);
    }

    public async Task<int> SendToKitchenAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Table)
            .Include(s => s.Waiter)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken)
            ?? throw new NotFoundException($"Buyurtma topilmadi: {saleId}");
        if (sale.Status != SaleStatus.Pending)
            throw new ValidationException("Faqat ochiq buyurtmani oshxonaga yuborish mumkin.");

        var newItems = sale.Items.Where(i => !i.SentToKitchen).ToList();
        if (newItems.Count == 0)
            return 0;

        var ticket = new KitchenTicket
        {
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            TableName = sale.Table?.Name,
            WaiterName = sale.Waiter?.Username
        };
        foreach (var item in newItems)
        {
            ticket.Items.Add(new KitchenTicketItem
            {
                DishName = item.ProductName,
                Quantity = item.Quantity,
                Note = item.Note
            });
            item.SentToKitchen = true;
        }

        _db.KitchenTickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        return newItems.Count;
    }

    public async Task<SaleDto> PayOrderAsync(Guid saleId, PayOrderRequest request, CancellationToken cancellationToken = default)
    {
        var cashierId = _currentUser.UserId
            ?? throw new UnauthorizedException("Joriy foydalanuvchi aniqlanmadi.");

        var sale = await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Table)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken)
            ?? throw new NotFoundException($"Buyurtma topilmadi: {saleId}");
        if (sale.Status != SaleStatus.Pending)
            throw new ValidationException("Bu buyurtma allaqachon yopilgan.");
        if (sale.Items.Count == 0)
            throw new ValidationException("Bo'sh buyurtmani yopib bo'lmaydi.");

        var shift = await _db.CashShifts
            .FirstOrDefaultAsync(s => s.CashierId == cashierId && s.Status == ShiftStatus.Open, cancellationToken)
            ?? throw new ValidationException("To'lov uchun avval smenani oching (kassa sessiyasi).");

        // Buyurtma ochilganda kassa noma'lum (stol oqimi) — to'lovni qabul qilgan kassa yoziladi.
        var payTerminal = ResolveTerminalId(request.TerminalId);
        if (payTerminal is not null)
            sale.TerminalId = payTerminal;

        Debtor? debtor = null;
        if (request.PaymentMethod == PaymentMethod.Debt)
        {
            if (request.DebtorId is null)
                throw new ValidationException("Nasiya uchun qarzdorni tanlang.");
            debtor = await _db.Debtors.FirstOrDefaultAsync(d => d.Id == request.DebtorId, cancellationToken)
                ?? throw new NotFoundException("Qarzdor topilmadi.");
        }

        Customer? customer = null;
        if (request.CustomerId.HasValue)
            customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        // Tovar (Good) qatorlari uchun ombordan ayiramiz; ovqat (Dish) ayirilmaydi.
        var goodIds = sale.Items
            .Where(i => i.ProductId != Guid.Empty)
            .Select(i => i.ProductId).Distinct().ToList();
        var goodProducts = await _db.Products
            .Where(p => goodIds.Contains(p.Id) && p.ProductType == ProductType.Good)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        if (goodProducts.Count > 0 && sale.BranchId.HasValue)
        {
            var stocks = await _db.BranchStocks
                .Where(s => s.BranchId == sale.BranchId && goodProducts.Keys.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, cancellationToken);

            // FIFO tannarx qatlamlari — eng eski partiya birinchi.
            var layersByProduct = await LoadCostLayersAsync(
                sale.BranchId.Value, goodProducts.Keys.ToList(), cancellationToken);

            foreach (var item in sale.Items.Where(i => goodProducts.ContainsKey(i.ProductId)))
            {
                if (!stocks.TryGetValue(item.ProductId, out var stock) || stock.Quantity < item.Quantity)
                    throw new ValidationException($"'{item.ProductName}' qoldig'i yetarli emas (mavjud: {stock?.Quantity ?? 0}).");
                stock.Quantity -= item.Quantity;

                layersByProduct.TryGetValue(item.ProductId, out var prodLayers);
                item.CostPrice = ConsumeFifoCost(prodLayers, item.Quantity, goodProducts[item.ProductId].CostPrice);
            }
        }

        RecalculateTotals(sale);

        // Servis haqi.
        sale.ServiceChargePercent = request.ServiceChargePercent;
        sale.ServiceChargeAmount = Round2(sale.Items.Sum(i => i.LineTotal) * request.ServiceChargePercent / 100m);
        sale.TotalAmount = Round2(sale.Items.Sum(i => i.LineTotal) + sale.ServiceChargeAmount);

        sale.CashShiftId = shift.Id;
        sale.CashierId = cashierId;
        sale.PaymentMethod = request.PaymentMethod;
        sale.DebtorId = debtor?.Id;
        sale.CustomerId = customer?.Id;

        if (request.PaymentMethod == PaymentMethod.Cash && request.PaidAmount < Math.Round(sale.TotalAmount, 0, MidpointRounding.AwayFromZero))
            throw new ValidationException("Berilgan summa savdo summasidan kam.");

        if (request.PaymentMethod == PaymentMethod.Debt)
        {
            sale.PaidAmount = 0;
            sale.ChangeAmount = 0;
        }
        else
        {
            sale.PaidAmount = request.PaidAmount;
            sale.ChangeAmount = Math.Max(0, Round2(request.PaidAmount - sale.TotalAmount));
        }
        ApplyCardAmount(sale, request.PaymentMethod, request.CardAmount);

        sale.Status = SaleStatus.Completed;
        // Mavjud (tracked) savdoga 1:1 chek — DbSet orqali aniq Added qilib, navigatsiyani ham bog'laymiz.
        var receipt = new Receipt { SaleId = sale.Id, FiscalStatus = FiscalStatus.NotSent };
        _db.Receipts.Add(receipt);
        sale.Receipt = receipt;

        if (customer is not null)
        {
            var points = Round2(sale.TotalAmount * 0.01m);
            customer.LoyaltyPoints += points;
            sale.LoyaltyPointsEarned = points;
            _db.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CustomerId = customer.Id,
                Points = points,
                Amount = sale.TotalAmount,
                Comment = $"Savdo {sale.ReceiptNumber}",
                Sale = sale
            });
        }

        if (debtor is not null)
        {
            debtor.Balance += sale.TotalAmount;
            _db.DebtorTransactions.Add(new DebtorTransaction
            {
                DebtorId = debtor.Id,
                Type = DebtTransactionType.Debt,
                Amount = sale.TotalAmount,
                BalanceAfter = debtor.Balance,
                Comment = $"Savdo {sale.ReceiptNumber}"
            });
        }

        // Stolni bo'shatamiz.
        if (sale.Table is not null)
            sale.Table.Status = TableStatus.Free;

        await _db.SaveChangesAsync(cancellationToken);

        // Restoran buyurtmasi to'lovi doim fiskal (yashil oqim) — nofiskal tanlov yo'q.
        await EnqueueFiscalIfNeededAsync(sale, fiscalize: true, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(sale);
    }

    public async Task CancelOrderAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales
            .Include(s => s.Table)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken)
            ?? throw new NotFoundException($"Buyurtma topilmadi: {saleId}");
        if (sale.Status != SaleStatus.Pending)
            throw new ValidationException("Faqat ochiq buyurtmani bekor qilish mumkin.");

        sale.Status = SaleStatus.Cancelled;
        if (sale.Table is not null)
            sale.Table.Status = TableStatus.Free;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void RecalculateTotals(Sale sale)
    {
        sale.TotalAmount = sale.Items.Sum(i => i.LineTotal);
        sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
        sale.VatAmount = sale.Items.Sum(i => i.VatAmount);
    }

    /// <summary>
    /// Chek raqami: {prefix}-yyyyMMdd-#### (kunlik ketma-ketlik, sotuv "R", refund "RR").
    /// Hisoblagich kunlik barcha savdolarga umumiy — prefiks bilan birga noyob bo'ladi.
    /// Eslatma: yuqori parallellikda poyga (race) bo'lishi mumkin — keyinroq DB ketma-ketligi
    /// (sequence) yoki filialga bog'langan hisoblagich bilan almashtiriladi.
    /// </summary>
    private async Task<string> GenerateReceiptNumberAsync(string prefix, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var countToday = await _db.Sales
            .CountAsync(s => s.CreatedAt >= today && s.CreatedAt < tomorrow, cancellationToken);

        return $"{prefix}-{today:yyyyMMdd}-{countToday + 1:D4}";
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Aralash to'lovning karta qismini tekshirib savdoga yozadi. Fiskal chekda to'lov shakli
    /// naqd/karta bo'yicha ajratilishi shart (VMQ-943), shuning uchun karta qismi 0 dan katta
    /// jamidan oshmasligi kerak. Aralash bo'lmagan to'lovda karta qismi 0.
    /// </summary>
    private static void ApplyCardAmount(Sale sale, PaymentMethod method, decimal cardAmount)
    {
        if (method != PaymentMethod.Mixed)
        {
            sale.CardAmount = 0;
            return;
        }

        var card = Round2(cardAmount);
        if (card > sale.TotalAmount)
            throw new ValidationException("Aralash to'lovda karta qismi jami summadan oshmasligi kerak.");

        // Eski kassa versiyalari (va ular oflayn navbatga qo'ygan savdolar) karta qismini
        // yubormaydi — avvalgidek butun summani karta deb hisoblaymiz, savdo rad etilmasin.
        sale.CardAmount = card > 0 ? card : sale.TotalAmount;
    }

    /// <summary>
    /// Berilgan filial va mahsulotlar uchun qoldig'i bor FIFO tannarx qatlamlarini
    /// (kirim partiyalari) eng eski birinchi tartibida yuklaydi. Qaytarilgan entitylar
    /// tracking'da — RemainingQuantity o'zgarishi SaveChanges'da saqlanadi.
    /// </summary>
    private async Task<Dictionary<Guid, List<StockReceiptItem>>> LoadCostLayersAsync(
        Guid branchId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, List<StockReceiptItem>>();

        var layers = await _db.StockReceiptItems
            .Where(li => li.RemainingQuantity > 0
                && productIds.Contains(li.ProductId)
                && li.StockReceipt!.BranchId == branchId)
            .OrderBy(li => li.CreatedAt)
            .ToListAsync(cancellationToken);

        return layers
            .GroupBy(li => li.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// FIFO bo'yicha tannarx qatlamlaridan <paramref name="qty"/> miqdorni "iste'mol" qiladi
    /// (eng eski qatlamdan boshlab RemainingQuantity'ni kamaytiradi) va sotilgan birlik uchun
    /// tortilgan o'rtacha tannarxni qaytaradi. Qatlam yetmasa (qatlamsiz eski qoldiq,
    /// ko'chirma yoki inventarizatsiya), qolgani <paramref name="fallbackCost"/> — mahsulotning
    /// joriy tannarxi — bo'yicha hisoblanadi.
    /// </summary>
    private static decimal ConsumeFifoCost(List<StockReceiptItem>? layers, decimal qty, decimal fallbackCost)
    {
        if (qty <= 0) return fallbackCost;

        decimal remaining = qty;
        decimal totalCost = 0m;

        if (layers is not null)
        {
            foreach (var layer in layers)
            {
                if (remaining <= 0) break;
                if (layer.RemainingQuantity <= 0) continue;

                var take = Math.Min(layer.RemainingQuantity, remaining);
                layer.RemainingQuantity -= take;
                totalCost += take * layer.CostPrice;
                remaining -= take;
            }
        }

        if (remaining > 0)
            totalCost += remaining * fallbackCost;

        return Round2(totalCost / qty);
    }
}
