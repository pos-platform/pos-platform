using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Dashboard.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DashboardService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        // Sanalar — Toshkent kuni; filialga biriktirilgan foydalanuvchi faqat o'z filialini ko'radi.
        var (fromDate, toExclusive) = BusinessTime.LocalRangeToUtc(
            from ?? BusinessTime.TodayLocal, to ?? BusinessTime.TodayLocal);
        branchId = _currentUser.ForRead(branchId);

        var sales = _db.Sales
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt < toExclusive
                        && s.Status != SaleStatus.Pending && s.Status != SaleStatus.Cancelled)
            .Where(s => branchId == null || s.BranchId == branchId);

        var saleQ = sales.Where(s => s.Type == SaleType.Sale);
        var refundQ = sales.Where(s => s.Type == SaleType.Refund);

        async Task<decimal> Sum(IQueryable<Domain.Entities.Sale> q) =>
            await q.SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;

        // Naqd/karta qismi: aralash (Mixed) to'lovda karta = CardAmount, naqd = qolgani.
        async Task<decimal> Cash(IQueryable<Domain.Entities.Sale> q) =>
            await q.SumAsync(s => (decimal?)(s.PaymentMethod == PaymentMethod.Cash ? s.TotalAmount
                : s.PaymentMethod == PaymentMethod.Mixed ? s.TotalAmount - s.CardAmount : 0m), cancellationToken) ?? 0m;
        async Task<decimal> Card(IQueryable<Domain.Entities.Sale> q) =>
            await q.SumAsync(s => (decimal?)(s.PaymentMethod == PaymentMethod.Card ? s.TotalAmount
                : s.PaymentMethod == PaymentMethod.Mixed ? s.CardAmount : 0m), cancellationToken) ?? 0m;

        var totalSales = await Sum(saleQ);
        var returns = await Sum(refundQ);
        var cashIncome = await Cash(saleQ) - await Cash(refundQ);
        var cardIncome = await Card(saleQ) - await Card(refundQ);
        var transferIncome = await Sum(saleQ.Where(s => s.PaymentMethod == PaymentMethod.Transfer))
            - await Sum(refundQ.Where(s => s.PaymentMethod == PaymentMethod.Transfer));
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
            NetSales = totalSales - returns,
            CashIncome = cashIncome,
            CardIncome = cardIncome,
            TransferIncome = transferIncome,
            Returns = returns,
            Debts = debts,
            Expenses = expenses,
            SalesCount = salesCount
        };
    }
}
