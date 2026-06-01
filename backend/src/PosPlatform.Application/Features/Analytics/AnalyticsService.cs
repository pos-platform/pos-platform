using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Analytics.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _db;

    public AnalyticsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsDto> GetAsync(DateTime? from, DateTime? to, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var toDefault = DateTime.UtcNow.Date;
        var fromDefault = toDefault.AddDays(-6); // oxirgi 7 kun
        var fromDate = DateTime.SpecifyKind((from ?? fromDefault).Date, DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind((to ?? toDefault).Date.AddDays(1), DateTimeKind.Utc);

        // Sotuvlar (Type=Sale) shu davrda.
        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.Type == SaleType.Sale && s.CreatedAt >= fromDate && s.CreatedAt < toExclusive)
            .Where(s => branchId == null || s.BranchId == branchId)
            .Select(s => new { s.CreatedAt, s.TotalAmount })
            .ToListAsync(cancellationToken);

        var revenue = sales.Sum(s => s.TotalAmount);
        var salesCount = sales.Count;

        // Qatorlar (tannarx/foyda va top tovarlar uchun).
        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.Type == SaleType.Sale
                        && i.Sale.CreatedAt >= fromDate && i.Sale.CreatedAt < toExclusive
                        && (branchId == null || i.Sale.BranchId == branchId))
            .Select(i => new { i.ProductId, i.ProductName, i.Quantity, i.LineTotal })
            .ToListAsync(cancellationToken);

        // Joriy tannarxlar (taxminiy COGS — sotuv paytidagi emas, joriy tannarx).
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var costs = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.CostPrice })
            .ToDictionaryAsync(p => p.Id, p => p.CostPrice, cancellationToken);

        var cost = items.Sum(i => (costs.TryGetValue(i.ProductId, out var c) ? c : 0m) * i.Quantity);
        var profit = revenue - cost;

        var dailySales = sales
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new DailySalesDto { Date = g.Key, Total = g.Sum(x => x.TotalAmount), Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToList();

        var topProducts = items
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToList();

        return new AnalyticsDto
        {
            Revenue = revenue,
            Cost = Math.Round(cost, 2),
            Profit = Math.Round(profit, 2),
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100m, 2) : 0m,
            SalesCount = salesCount,
            AvgCheck = salesCount > 0 ? Math.Round(revenue / salesCount, 2) : 0m,
            DailySales = dailySales,
            TopProducts = topProducts
        };
    }
}
