using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Analytics.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AnalyticsService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AnalyticsDto> GetAsync(DateTime? from, DateTime? to, Guid? branchId, CancellationToken cancellationToken = default)
    {
        // Sanalar — Toshkent kuni; standart — oxirgi 7 kun.
        var toDefault = BusinessTime.TodayLocal;
        var fromDefault = toDefault.AddDays(-6);
        var (fromDate, toExclusive) = BusinessTime.LocalRangeToUtc(from ?? fromDefault, to ?? toDefault);
        branchId = _currentUser.ForRead(branchId);

        // Sotuv va qaytarishlar (Pending/Cancelled kirmaydi). Qaytarish manfiy ishora bilan — sof tushum.
        var sales = await _db.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt < toExclusive
                        && s.Status != SaleStatus.Pending && s.Status != SaleStatus.Cancelled)
            .Where(s => branchId == null || s.BranchId == branchId)
            .Select(s => new { s.CreatedAt, s.TotalAmount, IsSale = s.Type == SaleType.Sale })
            .ToListAsync(cancellationToken);

        var revenue = sales.Sum(s => s.IsSale ? s.TotalAmount : -s.TotalAmount);
        var salesCount = sales.Count(s => s.IsSale);

        // Qatorlar (tannarx/foyda va top tovarlar uchun) — CostPrice snapshot'dan, qaytarish ayiriladi.
        var items = await _db.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.CreatedAt >= fromDate && i.Sale.CreatedAt < toExclusive
                        && i.Sale.Status != SaleStatus.Pending && i.Sale.Status != SaleStatus.Cancelled
                        && (branchId == null || i.Sale.BranchId == branchId))
            .Select(i => new
            {
                i.ProductId, i.ProductName, i.Quantity, i.LineTotal, i.CostPrice,
                Sign = i.Sale!.Type == SaleType.Refund ? -1m : 1m
            })
            .ToListAsync(cancellationToken);

        var cost = items.Sum(i => i.Sign * i.CostPrice * i.Quantity);
        var profit = revenue - cost;

        var dailySales = sales
            .GroupBy(s => BusinessTime.LocalDate(s.CreatedAt))
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                Total = g.Sum(x => x.IsSale ? x.TotalAmount : -x.TotalAmount),
                Count = g.Count(x => x.IsSale)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var topProducts = items
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                Quantity = g.Sum(x => x.Sign * x.Quantity),
                Revenue = g.Sum(x => x.Sign * x.LineTotal)
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
