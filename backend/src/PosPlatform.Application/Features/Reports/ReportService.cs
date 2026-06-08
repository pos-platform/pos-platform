using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Reports.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Reports;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly IReportExcelService _excel;

    public ReportService(IApplicationDbContext db, IReportExcelService excel)
    {
        _db    = db;
        _excel = excel;
    }

    // ─── Savdolar hisoboti ────────────────────────────────────────────────────

    public async Task<SalesReportDto> GetSalesReportAsync(
        DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default)
    {
        var fromUtc = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toUtc   = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);

        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
            .Where(s => branchId == null || s.BranchId == branchId)
            .Select(s => new { s.CreatedAt, s.Type, s.Status, s.PaymentMethod, s.TotalAmount })
            .ToListAsync(ct);

        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.CreatedAt >= fromUtc && i.Sale.CreatedAt < toUtc
                        && i.Sale.Type == SaleType.Sale
                        && (branchId == null || i.Sale.BranchId == branchId))
            .Select(i => new { i.ProductName, i.Quantity, i.LineTotal, i.CostPrice, i.Sale!.CreatedAt })
            .ToListAsync(ct);

        var revenue      = sales.Where(s => s.Type == SaleType.Sale).Sum(s => s.TotalAmount);
        var refundAmount = sales.Where(s => s.Type == SaleType.Refund).Sum(s => s.TotalAmount);
        var cost         = items.Sum(i => i.CostPrice * i.Quantity);
        var profit       = revenue - cost;

        var days = items
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new SalesReportDayDto
            {
                Date    = g.Key,
                Count   = sales.Count(s => s.CreatedAt.Date == g.Key && s.Type == SaleType.Sale),
                Revenue = g.Sum(i => i.LineTotal),
                Cost    = g.Sum(i => i.CostPrice * i.Quantity),
                Profit  = g.Sum(i => i.LineTotal) - g.Sum(i => i.CostPrice * i.Quantity)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var topProducts = items
            .GroupBy(i => i.ProductName)
            .Select(g => new SalesReportProductDto
            {
                ProductName = g.Key,
                Quantity    = g.Sum(i => i.Quantity),
                Revenue     = g.Sum(i => i.LineTotal),
                Cost        = g.Sum(i => i.CostPrice * i.Quantity),
                Profit      = g.Sum(i => i.LineTotal) - g.Sum(i => i.CostPrice * i.Quantity)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(20)
            .ToList();

        return new SalesReportDto
        {
            From          = from,
            To            = to,
            TotalCount    = sales.Count(s => s.Type == SaleType.Sale),
            TotalRevenue  = revenue,
            TotalCost     = Math.Round(cost, 2),
            TotalProfit   = Math.Round(profit, 2),
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0,
            CashAmount    = sales.Where(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.TotalAmount),
            CardAmount    = sales.Where(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Card).Sum(s => s.TotalAmount),
            DebtAmount    = sales.Where(s => s.Type == SaleType.Sale && s.PaymentMethod == PaymentMethod.Debt).Sum(s => s.TotalAmount),
            RefundAmount  = refundAmount,
            Days          = days,
            TopProducts   = topProducts
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
        IQueryable<Domain.Entities.Product> query = _db.Products
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
