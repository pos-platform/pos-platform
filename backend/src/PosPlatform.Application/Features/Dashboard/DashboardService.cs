using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Dashboard.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.SpecifyKind((from ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind((to ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);

        var sales = _db.Sales
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt < toExclusive)
            .Where(s => branchId == null || s.BranchId == branchId);

        var saleQ = sales.Where(s => s.Type == SaleType.Sale);
        var refundQ = sales.Where(s => s.Type == SaleType.Refund);

        async Task<decimal> Sum(IQueryable<Domain.Entities.Sale> q) =>
            await q.SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        var totalSales = await Sum(saleQ);
        var cashSales = await Sum(saleQ.Where(s => s.PaymentMethod == PaymentMethod.Cash));
        var cardSales = await Sum(saleQ.Where(s => s.PaymentMethod == PaymentMethod.Card));
        var returns = await Sum(refundQ);
        var cashRefunds = await Sum(refundQ.Where(s => s.PaymentMethod == PaymentMethod.Cash));
        var cardRefunds = await Sum(refundQ.Where(s => s.PaymentMethod == PaymentMethod.Card));
        var salesCount = await saleQ.CountAsync(cancellationToken);

        var expenses = await _db.Expenses
            .Where(e => e.CreatedAt >= fromDate && e.CreatedAt < toExclusive)
            .Where(e => branchId == null || e.BranchId == branchId)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        // Qarz — joriy umumiy holat (davrga bog'liq emas).
        var debts = await _db.Debtors.SumAsync(d => (decimal?)d.Balance, cancellationToken) ?? 0m;

        return new DashboardSummaryDto
        {
            TotalSales = totalSales,
            CashIncome = cashSales - cashRefunds,
            CardIncome = cardSales - cardRefunds,
            Returns = returns,
            Debts = debts,
            Expenses = expenses,
            SalesCount = salesCount
        };
    }
}
