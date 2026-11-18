using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.Promotions;
using PosCloud.Application.Features.Sales.Dtos;
using PosCloud.Application.Features.Stock;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Sales;

public partial class SaleService : ISaleService
{
    /// <summary>O'zbekiston standart QQS stavkasi (%).</summary>
    private const decimal DefaultVatPercent = 12m;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFiscalReceiptService _fiscal;
    private readonly ISystemErrorRecorder _errors;

    public SaleService(IApplicationDbContext db, ICurrentUser currentUser, IFiscalReceiptService fiscal,
        ISystemErrorRecorder errors)
    {
        _db = db;
        _currentUser = currentUser;
        _fiscal = fiscal;
        _errors = errors;
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

        // Filialga biriktirilgan foydalanuvchi faqat o'z filiali savdolarini ko'radi.
        branchId = _currentUser.ForRead(branchId);
        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId);

        // Sana chegaralari — Toshkent kuni: [from, to) (to — kirmaydigan kun).
        if (from.HasValue)
        {
            var fromUtc = BusinessTime.LocalDayStartUtc(from.Value);
            query = query.Where(s => s.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = BusinessTime.LocalDayStartUtc(to.Value);
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

    public async Task<SaleDto> CreateWithRetryAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateAsync(request, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < 5)
            {
                // Boshqa kassa shu tovar qoldig'ini ayni paytda o'zgartirdi — hech narsa saqlanmagan,
                // eskirgan entitylarni unutib, yangi qoldiq bilan qaytadan hisoblaymiz.
                _db.ClearChangeTracker();
            }
            catch (DbUpdateException ex) when (attempt < 5 && IsReceiptNumberConflict(ex))
            {
                // Parallel savdo xuddi shu chek raqamini oldi — keyingi raqam bilan qaytadan.
                _db.ClearChangeTracker();
            }
        }
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

        // Kassir faqat o'z filialiga sota oladi; filial shu tashkilotniki bo'lishi shart.
        var saleBranchId = await _currentUser.ResolveForWriteAsync(_db, request.BranchId, cancellationToken)
            ?? throw new ValidationException("Savdo uchun filial aniqlanmadi.");

        // Savdo vaqti: oflayn navbatdan kelgan savdo — kassada yakunlangan haqiqiy vaqt (oqilona chegarada),
        // aks holda server vaqti. Aksiyalar ham shu vaqt bo'yicha qo'llanadi (kassa ham shunday hisoblagan).
        var nowUtc = DateTime.UtcNow;
        var saleTimeUtc = ResolveSaleTime(request.ClientCreatedAt, nowUtc);

        // Kassadan kelgan savdo (ClientSaleId, online buyurtma emas) qoldiq yetmasa ham qabul qilinadi:
        // tovar mijozga berilgan, pul olingan — rad etsak savdo umuman yo'qoladi. Qoldiq manfiyga tushadi,
        // savdo belgilanadi va administratorga ogohlantirish yoziladi.
        var allowShortage = clientSaleId is not null
            && !clientSaleId.StartsWith(LoyaltyRules.OnlineSalePrefix, StringComparison.Ordinal);
        var shortages = new List<string>();

        var branchStocks = await _db.BranchStocks
            .Where(s => s.BranchId == saleBranchId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        // FIFO tannarx qatlamlari — shu filialning qoldig'i bor kirim partiyalari, eng eski birinchi.
        var layersByProduct = await LoadCostLayersAsync(saleBranchId, productIds, cancellationToken);

        // Amaldagi aksiyalar (savdo vaqtida) — chegirma server tomonida hisoblanadi.
        var activePromotions = await _db.Promotions
            .Where(p => p.IsActive && p.StartDate <= saleTimeUtc && p.EndDate >= saleTimeUtc)
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
            BranchId = saleBranchId,
            CreatedAt = saleTimeUtc,
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

        // Ball hisoblash uchun: qator, mahsulot va narxi pasaytirilganmi (online aksiya narxi qotirilgan).
        var earnSource = new List<(SaleItem Item, Product Product, bool PriceReduced)>();
        var stockMovements = new List<StockMovement>();

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
            var promoPercent = request.SkipPromotions
                ? 0m
                : PromotionCalculator.GetDiscountPercent(activePromotions, product.Id, product.CategoryId);
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
                var available = branchStocks.TryGetValue(product.Id, out var existingStock) ? existingStock.Quantity : 0m;
                if (available < line.Quantity)
                {
                    if (!allowShortage)
                        throw new ValidationException($"'{product.Name}' qoldig'i yetarli emas (mavjud: {available}).");
                    shortages.Add($"{product.Name}: sotildi {line.Quantity}, qoldiq {available}");
                }
                var stock = StockLedger.GetOrCreate(_db, branchStocks, saleBranchId, product.Id);

                layersByProduct.TryGetValue(product.Id, out var prodLayers);
                costPrice = ConsumeFifoCost(prodLayers, line.Quantity, product.CostPrice);

                // Qoldiq ombor harakati jurnali orqali kamayadi (chek raqami pastda, raqam berilgach yoziladi).
                stockMovements.Add(StockLedger.Apply(_db, stock, -line.Quantity, StockMovementType.Sale,
                    new StockDoc(sale.Id, null, cashierId), costPrice));
            }
            else
            {
                costPrice = product.CostPrice;
            }

            var saleItem = new SaleItem
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
            };
            sale.Items.Add(saleItem);
            earnSource.Add((saleItem, product, request.SkipPromotions && unitPrice < product.Price));
        }

        // Bonus sozlamalari — mijoz tashkiloti bo'yicha.
        var loyalty = customer is not null
            ? await LoyaltyRules.LoadAsync(_db, customer.OrganizationId, cancellationToken)
            : null;

        // Ball bilan to'lov: qatorlarga chegirma sifatida mutanosib taqsimlanadi (fiskal chek to'g'ri bo'lishi uchun),
        // savdo jami va QQS shundan KEYIN hisoblanadi. Oflayn navbatdagi savdo kech kelib balans yetmasa —
        // savdo rad etilmaydi, ball mavjud miqdorgacha qisqartiriladi (tranzaksiya izohida yoziladi).
        var redeemRequested = Math.Max(0, Math.Floor(request.RedeemPoints ?? 0));
        decimal redeemed = 0;
        if (redeemRequested > 0)
        {
            if (request.CustomerId is null)
                throw new ValidationException("Ball bilan to'lash uchun mijozni tanlang.");

            var preTotal = sale.Items.Sum(i => i.LineTotal);
            var balance = Math.Floor(Math.Max(0, customer?.LoyaltyPoints ?? 0));
            var byPercent = Math.Floor(preTotal * (loyalty?.MaxRedeemPercent ?? 0) / 100m);
            redeemed = Math.Max(0, Math.Min(Math.Min(redeemRequested, balance), Math.Min(byPercent, Math.Floor(preTotal))));

            if (redeemed > 0)
            {
                var items = sale.Items.ToList();
                var shares = OnlineOrderPricing.Distribute(items.Select(i => i.LineTotal).ToList(), redeemed);
                for (var k = 0; k < items.Count; k++)
                {
                    if (shares[k] <= 0) continue;
                    var it = items[k];
                    it.DiscountAmount = Round2(it.DiscountAmount + shares[k]);
                    it.LineTotal = Round2(it.LineTotal - shares[k]);
                    it.VatAmount = Round2(it.LineTotal * it.VatPercent / (100m + it.VatPercent));
                }
            }
        }
        // Ball qisqartirilgan bo'lsa kassa kamroq summa hisoblagan — naqd tekshiruvida shu farqqa yo'l qo'yamiz.
        var redeemShortfall = redeemRequested - redeemed;

        sale.TotalAmount = sale.Items.Sum(i => i.LineTotal);
        sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
        sale.VatAmount = sale.Items.Sum(i => i.VatAmount);

        // Kassada tiyin ishlatilmaydi — tarozili mahsulotlar tufayli jami kasrli bo'lsa
        // (masalan 223 070,4), butun so'mga yaxlitlangan qiymat bilan solishtiramiz.
        if (request.PaymentMethod == PaymentMethod.Cash
            && request.PaidAmount + redeemShortfall < Math.Round(sale.TotalAmount, 0, MidpointRounding.AwayFromZero))
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

        sale.StockShortage = shortages.Count > 0;
        sale.ReceiptNumber = await GenerateReceiptNumberAsync("R", saleTimeUtc, cancellationToken);
        foreach (var m in stockMovements) m.DocumentNumber = sale.ReceiptNumber;

        sale.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        _db.Sales.Add(sale);

        // Bonus: ball bilan to'lov (Redeem) va cashback (Earn) — sozlamalar bo'yicha (LoyaltyRules).
        if (customer is not null && loyalty is not null)
        {
            var source = LoyaltyRules.SourceFor(clientSaleId);
            if (redeemed > 0)
            {
                var note = redeemShortfall > 0
                    ? $" (so'ralgan {redeemRequested:0}, balans/chegara bo'yicha {redeemed:0} ga qisqartirildi)"
                    : string.Empty;
                sale.LoyaltyPointsRedeemed = -LoyaltyRules.Apply(_db, customer, -redeemed, LoyaltyTransactionType.Redeem,
                    source, redeemed, $"Savdo {sale.ReceiptNumber} — ball bilan to'lov{note}", sale);
            }

            // Online buyurtma savdosida cashback "Tayyor"da emas — mijoz qabul qilganda (Delivered) yoziladi
            // (OnlineOrderCashback). Bekor qilinsa ball umuman yozilmaydi.
            if (source != LoyaltySource.Online)
                sale.LoyaltyPointsEarned = await EarnPointsAsync(loyalty, sale, earnSource
                    .Select(e => new LoyaltyEarnLine(e.Product.CategoryId, e.Product.ProductType, e.Item.LineTotal,
                        e.Item.DiscountAmount > 0 || e.PriceReduced)), customer, source, cancellationToken);
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

        if (shortages.Count > 0)
        {
            await _errors.RecordAsync(new SystemErrorEntry
            {
                Source = SystemErrorSource.Desktop,
                Level = SystemErrorLevel.Warning,
                Message = $"Savdo {sale.ReceiptNumber} qoldiq yetmasa ham qabul qilindi (kassa savdosi) — qoldiq manfiy",
                Details = string.Join("\n", shortages),
                ErrorType = "StockShortage",
                Location = "SaleService.CreateAsync",
                OrganizationId = sale.OrganizationId,
                BranchId = sale.BranchId,
                UserId = cashierId,
                UserName = _currentUser.Username
            }, cancellationToken);
        }

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

    /// <summary>
    /// Chek raqami: {prefix}-yyyyMMdd-#### — tashkilot ichida, Toshkent kuni bo'yicha ketma-ket
    /// (sotuv "R", qaytarish "RR"). Noyoblik (OrganizationId, ReceiptNumber) indeksi bilan
    /// kafolatlanadi; parallel savdo bir xil raqam olsa CreateWithRetryAsync qaytadan urinadi.
    /// </summary>
    private async Task<string> GenerateReceiptNumberAsync(string prefix, DateTime atUtc, CancellationToken cancellationToken)
    {
        var stem = $"{prefix}-{BusinessTime.LocalDate(atUtc):yyyyMMdd}-";

        // Tenant filtri tufayli faqat joriy tashkilot cheklari ko'riladi.
        var existing = await _db.Sales
            .Where(s => s.ReceiptNumber.StartsWith(stem))
            .Select(s => s.ReceiptNumber)
            .ToListAsync(cancellationToken);

        var max = existing
            .Select(n => int.TryParse(n.AsSpan(stem.Length), out var k) ? k : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{stem}{max + 1:D4}";
    }

    /// <summary>Chek raqami noyoblik indeksi buzilganmi (parallel savdo bir xil raqam olgan).</summary>
    private static bool IsReceiptNumberConflict(DbUpdateException ex) =>
        (ex.InnerException?.Message ?? ex.Message).Contains("ReceiptNumber", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Kassa yuborgan savdo vaqti: kelajakda 5 daqiqadan ko'p yoki 30 kundan eski bo'lsa e'tiborga
    /// olinmaydi (kassa soati noto'g'ri) — server vaqti olinadi.
    /// </summary>
    private static DateTime ResolveSaleTime(DateTime? clientCreatedAt, DateTime nowUtc)
    {
        if (clientCreatedAt is not { } t) return nowUtc;
        var utc = t.Kind switch
        {
            DateTimeKind.Local => t.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(t, DateTimeKind.Utc),
            _ => t
        };
        if (utc > nowUtc.AddMinutes(5) || utc < nowUtc.AddDays(-30)) return nowUtc;
        return utc;
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Cashback ballini hisoblab yozadi (Earn). Yozilgan ballni qaytaradi.</summary>
    private async Task<decimal> EarnPointsAsync(EffectiveLoyaltySettings loyalty, Sale sale, IEnumerable<LoyaltyEarnLine> lines,
        Customer customer, LoyaltySource source, CancellationToken ct)
    {
        // Mijoz darajasi (oxirgi 365 kun xaridi) — foiz bazaviy/kategoriya foizidan katta bo'lsa qo'llanadi.
        var tier = await LoyaltyRules.TierStatusAsync(_db, loyalty, customer.Id, ct);
        var points = LoyaltyRules.ComputeEarn(loyalty, sale.TotalAmount, lines, tier.Tier?.CashbackPercent ?? 0);
        if (points <= 0) return 0;
        return LoyaltyRules.Apply(_db, customer, points, LoyaltyTransactionType.Earn, source, sale.TotalAmount,
            $"Savdo {sale.ReceiptNumber}", sale);
    }

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
