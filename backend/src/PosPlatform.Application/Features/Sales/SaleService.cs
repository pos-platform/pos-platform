using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Promotions;
using PosPlatform.Application.Features.Sales.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Sales;

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

    public async Task<IReadOnlyList<SaleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sales = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
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

        // Mahsulotlarni bitta so'rovda yuklaymiz.
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Amaldagi aksiyalar — chegirma server tomonida hisoblanadi (mijozga ishonmaymiz).
        var nowUtc = DateTime.UtcNow;
        var activePromotions = await _db.Promotions
            .Where(p => p.IsActive && p.StartDate <= nowUtc && p.EndDate >= nowUtc)
            .ToListAsync(cancellationToken);

        var sale = new Sale
        {
            BranchId = request.BranchId ?? _currentUser.BranchId,
            CashierId = cashierId,
            CashShiftId = shift.Id,
            Type = SaleType.Sale,
            PaymentMethod = request.PaymentMethod,
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

            var unitPrice = line.UnitPriceOverride ?? product.Price;
            var vatPercent = line.VatPercent ?? DefaultVatPercent;

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

            sale.Items.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                MxikCode = product.MxikCode,
                Barcode = product.Barcode,
                Unit = product.Unit,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                DiscountAmount = discount,
                VatPercent = vatPercent,
                VatAmount = vatAmount,
                LineTotal = lineTotal
            });
        }

        sale.TotalAmount = sale.Items.Sum(i => i.LineTotal);
        sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
        sale.VatAmount = sale.Items.Sum(i => i.VatAmount);

        if (request.PaymentMethod == PaymentMethod.Cash && request.PaidAmount < sale.TotalAmount)
        {
            throw new ValidationException("Berilgan summa savdo summasidan kam.");
        }

        sale.PaidAmount = request.PaidAmount;
        sale.ChangeAmount = Math.Max(0, Round2(request.PaidAmount - sale.TotalAmount));

        sale.ReceiptNumber = await GenerateReceiptNumberAsync("R", cancellationToken);

        sale.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(cancellationToken);

        // Fiskalizatsiya seam'i — hozir placeholder, keyin lokal agent ulanadi.
        await _fiscal.EnqueueAsync(sale, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(sale);
    }

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
            OriginalSaleId = original.Id,
            PaymentMethod = original.PaymentMethod,
            Status = SaleStatus.Completed,
            TotalAmount = original.TotalAmount,
            DiscountAmount = original.DiscountAmount,
            VatAmount = original.VatAmount,
            PaidAmount = 0,
            ChangeAmount = 0,
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
                Unit = item.Unit,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                VatPercent = item.VatPercent,
                VatAmount = item.VatAmount,
                LineTotal = item.LineTotal
            });
        }

        refund.Receipt = new Receipt { FiscalStatus = FiscalStatus.NotSent };

        original.Status = SaleStatus.Refunded;

        _db.Sales.Add(refund);
        await _db.SaveChangesAsync(cancellationToken);

        // Return cheki ham fiskalizatsiya navbatiga.
        await _fiscal.EnqueueAsync(refund, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return SaleDto.FromEntity(refund);
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
}
