using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Reports.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Reports;

public partial class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly IReportExcelService _excel;
    private readonly ICurrentUser _currentUser;

    public ReportService(IApplicationDbContext db, IReportExcelService excel, ICurrentUser currentUser)
    {
        _db    = db;
        _excel = excel;
        _currentUser = currentUser;
    }

    // ─── Savdolar hisoboti ────────────────────────────────────────────────────

    public async Task<SalesReportDto> GetSalesReportAsync(
        DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default)
    {
        // Sanalar — Toshkent kuni (00:00–05:00 savdolari oldingi kunga tushmasin).
        var (fromUtc, toUtc) = BusinessTime.LocalRangeToUtc(from, to);
        branchId = _currentUser.ForRead(branchId);

        // Pending/Cancelled (yakunlanmagan/bekor) savdolar hisobotga kirmaydi.
        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc
                        && s.Status != SaleStatus.Pending && s.Status != SaleStatus.Cancelled)
            .Where(s => branchId == null || s.BranchId == branchId)
            .Select(s => new { s.CreatedAt, s.Type, s.PaymentMethod, s.TotalAmount, s.CardAmount })
            .ToListAsync(ct);

        // Qaytarish qatorlari manfiy ishora bilan — tushum, tannarx va foyda SOF (net) hisoblanadi
        // (sotilgan tovarlar logi bilan bir xil).
        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.CreatedAt >= fromUtc && i.Sale.CreatedAt < toUtc
                        && i.Sale.Status != SaleStatus.Pending && i.Sale.Status != SaleStatus.Cancelled
                        && (branchId == null || i.Sale.BranchId == branchId))
            .Select(i => new
            {
                i.ProductName, i.Quantity, i.LineTotal, i.CostPrice, i.Sale!.CreatedAt,
                Sign = i.Sale.Type == SaleType.Refund ? -1m : 1m
            })
            .ToListAsync(ct);

        static decimal Signed(SaleType type, decimal amount) => type == SaleType.Refund ? -amount : amount;

        var grossRevenue = sales.Where(s => s.Type == SaleType.Sale).Sum(s => s.TotalAmount);
        var refundAmount = sales.Where(s => s.Type == SaleType.Refund).Sum(s => s.TotalAmount);
        var revenue      = grossRevenue - refundAmount;
        var cost         = items.Sum(i => i.Sign * i.CostPrice * i.Quantity);
        var profit       = revenue - cost;

        // To'lov turlari bo'yicha (sof, qaytarishlar ayirilgan). Aralash to'lov: karta qismi — CardAmount,
        // naqd qismi — qolgani.
        decimal CashPart(PaymentMethod m, decimal total, decimal card) =>
            m == PaymentMethod.Cash ? total : m == PaymentMethod.Mixed ? total - card : 0m;
        decimal CardPart(PaymentMethod m, decimal total, decimal card) =>
            m == PaymentMethod.Card ? total : m == PaymentMethod.Mixed ? card : 0m;

        var days = items
            .GroupBy(i => BusinessTime.LocalDate(i.CreatedAt))
            .Select(g => new SalesReportDayDto
            {
                Date    = g.Key,
                Count   = sales.Count(s => BusinessTime.LocalDate(s.CreatedAt) == g.Key && s.Type == SaleType.Sale),
                Revenue = g.Sum(i => i.Sign * i.LineTotal),
                Cost    = Math.Round(g.Sum(i => i.Sign * i.CostPrice * i.Quantity), 2),
                Profit  = Math.Round(g.Sum(i => i.Sign * (i.LineTotal - i.CostPrice * i.Quantity)), 2)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var topProducts = items
            .GroupBy(i => i.ProductName)
            .Select(g => new SalesReportProductDto
            {
                ProductName = g.Key,
                Quantity    = g.Sum(i => i.Sign * i.Quantity),
                Revenue     = g.Sum(i => i.Sign * i.LineTotal),
                Cost        = Math.Round(g.Sum(i => i.Sign * i.CostPrice * i.Quantity), 2),
                Profit      = Math.Round(g.Sum(i => i.Sign * (i.LineTotal - i.CostPrice * i.Quantity)), 2)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(20)
            .ToList();

        return new SalesReportDto
        {
            From           = from,
            To             = to,
            TotalCount     = sales.Count(s => s.Type == SaleType.Sale),
            GrossRevenue   = grossRevenue,
            TotalRevenue   = revenue,
            TotalCost      = Math.Round(cost, 2),
            TotalProfit    = Math.Round(profit, 2),
            MarginPercent  = revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0,
            CashAmount     = sales.Sum(s => Signed(s.Type, CashPart(s.PaymentMethod, s.TotalAmount, s.CardAmount))),
            CardAmount     = sales.Sum(s => Signed(s.Type, CardPart(s.PaymentMethod, s.TotalAmount, s.CardAmount))),
            DebtAmount     = sales.Where(s => s.PaymentMethod == PaymentMethod.Debt).Sum(s => Signed(s.Type, s.TotalAmount)),
            TransferAmount = sales.Where(s => s.PaymentMethod == PaymentMethod.Transfer).Sum(s => Signed(s.Type, s.TotalAmount)),
            RefundAmount   = refundAmount,
            Days           = days,
            TopProducts    = topProducts
        };
    }

    public async Task<byte[]> ExportSalesReportAsync(
        DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default)
    {
        var report = await GetSalesReportAsync(from, to, branchId, ct);
        return _excel.ExportSalesReport(report);
    }

    // ─── Qoldiq hisoboti ─────────────────────────────────────────────────────

    public async Task<StockReportDto> GetStockReportAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        branchId = _currentUser.ForRead(branchId);
        IQueryable<Domain.Entities.Product> query = _db.Products.Where(p => p.ProductType != Domain.Enums.ProductType.Service)
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Stocks);

        if (branchId is not null)
            query = query.Where(p => p.BranchId == branchId || p.BranchId == null);

        var products = await query.OrderBy(p => p.Name).ToListAsync(ct);

        var items = products.Select(p =>
        {
            var qty = branchId.HasValue
                ? p.Stocks.FirstOrDefault(s => s.BranchId == branchId)?.Quantity ?? 0m
                : p.Stocks.Sum(s => s.Quantity);
            return new StockReportItemDto
            {
                Name      = p.Name,
                Category  = p.Category?.Name ?? "—",
                Barcode   = p.Barcode,
                Unit      = p.Unit == Domain.Enums.Unit.Kilogram ? "kg" : "dona",
                Quantity  = qty,
                CostPrice = p.CostPrice,
                SalePrice = p.Price,
                CostValue = Math.Round(qty * p.CostPrice, 2),
                SaleValue = Math.Round(qty * p.Price, 2)
            };
        }).ToList();

        return new StockReportDto
        {
            TotalProducts  = items.Count,
            TotalCostValue = items.Sum(i => i.CostValue),
            TotalSaleValue = items.Sum(i => i.SaleValue),
            Items          = items
        };
    }

    public async Task<byte[]> ExportStockReportAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var report = await GetStockReportAsync(branchId, ct);
        return _excel.ExportStockReport(report);
    }
}
