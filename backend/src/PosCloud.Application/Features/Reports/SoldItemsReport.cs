using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.Reports.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Reports;

/// <summary>
/// Sotilgan tovarlar logi (10.3): alohida jadval yo'q — har SaleItem bitta qator. Pending/Cancelled savdolar kirmaydi.
/// Manba: ClientSaleId "online:" bilan boshlansa — Online (refundda asl savdo kaliti bo'yicha).
/// </summary>
public partial class ReportService
{
    public const int SoldItemsMaxPageSize = 500;
    public const int SoldItemsMaxExportRows = 50_000;

    public async Task<SoldItemsPageDto> GetSoldItemsAsync(SoldItemsQuery q, CancellationToken ct = default)
    {
        var query = BuildSoldItemsQuery(q);
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize <= 0 ? 50 : q.PageSize, 1, SoldItemsMaxPageSize);

        var totalsRaw = await query
            .GroupBy(i => i.Sale!.Type)
            .Select(g => new
            {
                Type = g.Key,
                Qty = g.Sum(i => i.Quantity),
                Sum = g.Sum(i => i.LineTotal),
                Cost = g.Sum(i => i.CostPrice * i.Quantity)
            })
            .ToListAsync(ct);
        var sale = totalsRaw.FirstOrDefault(x => x.Type == SaleType.Sale);
        var refund = totalsRaw.FirstOrDefault(x => x.Type == SaleType.Refund);

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(i => i.Sale!.CreatedAt).ThenBy(i => i.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize), ct);

        return new SoldItemsPageDto
        {
            Items = items,
            Total = total,
            Totals = new SoldItemsTotalsDto
            {
                Quantity = (sale?.Qty ?? 0) - (refund?.Qty ?? 0),
                Revenue = (sale?.Sum ?? 0) - (refund?.Sum ?? 0),
                Profit = ((sale?.Sum ?? 0) - (sale?.Cost ?? 0)) - ((refund?.Sum ?? 0) - (refund?.Cost ?? 0)),
                Refunds = refund?.Sum ?? 0
            }
        };
    }

    public async Task<byte[]> ExportSoldItemsAsync(SoldItemsQuery q, CancellationToken ct = default)
    {
        var query = BuildSoldItemsQuery(q);
        if (await query.CountAsync(ct) > SoldItemsMaxExportRows)
            throw new ValidationException($"Juda ko'p qator (>{SoldItemsMaxExportRows:N0}). Sana oralig'ini qisqartiring.");
        var items = await Project(query.OrderByDescending(i => i.Sale!.CreatedAt).ThenBy(i => i.CreatedAt), ct);
        return _excel.ExportSoldItems(items);
    }

    private IQueryable<SaleItem> BuildSoldItemsQuery(SoldItemsQuery q)
    {
        var query = _db.SaleItems.AsNoTracking()
            .Where(i => i.Sale!.Status != SaleStatus.Pending && i.Sale.Status != SaleStatus.Cancelled);

        // Sana chegaralari — Toshkent kuni; "to" kuni ham kiradi. Standart — bugun.
        var (fromUtc, toUtc) = BusinessTime.LocalRangeToUtc(
            q.From ?? BusinessTime.TodayLocal, q.To ?? q.From ?? BusinessTime.TodayLocal);
        query = query.Where(i => i.Sale!.CreatedAt >= fromUtc && i.Sale.CreatedAt < toUtc);

        if (_currentUser.ForRead(q.BranchId) is { } branchId) query = query.Where(i => i.Sale!.BranchId == branchId);
        if (q.ProductId is { } productId) query = query.Where(i => i.ProductId == productId);
        if (q.CategoryId is { } categoryId) query = query.Where(i => i.Product!.CategoryId == categoryId);
        if (q.CashierId is { } cashierId) query = query.Where(i => i.Sale!.CashierId == cashierId);
        if (q.CustomerId is { } customerId) query = query.Where(i => i.Sale!.CustomerId == customerId);

        if (string.Equals(q.Type, "Sale", StringComparison.OrdinalIgnoreCase))
            query = query.Where(i => i.Sale!.Type == SaleType.Sale);
        else if (string.Equals(q.Type, "Refund", StringComparison.OrdinalIgnoreCase))
            query = query.Where(i => i.Sale!.Type == SaleType.Refund);

        const string prefix = LoyaltyRules.OnlineSalePrefix;
        if (string.Equals(q.Source, "Online", StringComparison.OrdinalIgnoreCase))
            query = query.Where(i => (i.Sale!.ClientSaleId ?? i.Sale.OriginalSale!.ClientSaleId ?? "").StartsWith(prefix));
        else if (string.Equals(q.Source, "Pos", StringComparison.OrdinalIgnoreCase))
            query = query.Where(i => !(i.Sale!.ClientSaleId ?? i.Sale.OriginalSale!.ClientSaleId ?? "").StartsWith(prefix));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(i => i.ProductName.ToLower().Contains(s)
                                     || (i.Barcode != null && i.Barcode.Contains(s))
                                     || i.Sale!.ReceiptNumber.ToLower().Contains(s));
        }
        return query;
    }

    private async Task<List<SoldItemDto>> Project(IQueryable<SaleItem> query, CancellationToken ct)
    {
        const string prefix = LoyaltyRules.OnlineSalePrefix;
        var rows = await query
            .Select(i => new
            {
                i.SaleId,
                i.Id,
                SoldAt = i.Sale!.CreatedAt,
                i.Sale.ReceiptNumber,
                i.Sale.Type,
                IsOnline = (i.Sale.ClientSaleId ?? i.Sale.OriginalSale!.ClientSaleId ?? "").StartsWith(prefix),
                BranchName = i.Sale.Branch != null ? i.Sale.Branch.Name : null,
                CashierName = i.Sale.Cashier != null ? (i.Sale.Cashier.FullName ?? i.Sale.Cashier.Username) : null,
                CustomerName = i.Sale.Customer != null ? i.Sale.Customer.Name : null,
                i.ProductId,
                i.ProductName,
                i.Barcode,
                i.Quantity,
                i.RefundedQuantity,
                i.UnitPrice,
                i.DiscountAmount,
                i.LineTotal,
                i.CostPrice,
                i.Sale.PaymentMethod,
                Points = i.Sale.Type == SaleType.Refund ? -i.Sale.LoyaltyPointsReversed : i.Sale.LoyaltyPointsEarned,
                SaleLinesTotal = i.Sale.Items.Sum(x => x.LineTotal),
                FiscalStatus = i.Sale.Receipt != null ? (FiscalStatus?)i.Sale.Receipt.FiscalStatus : null,
                OnlineOrderNumber = _db.OnlineOrders
                    .Where(o => o.SaleId == (i.Sale.OriginalSaleId ?? i.SaleId))
                    .Select(o => (int?)o.Number)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            var sign = r.Type == SaleType.Refund ? -1 : 1;
            return new SoldItemDto
            {
                SaleId = r.SaleId,
                SaleItemId = r.Id,
                SoldAt = r.SoldAt,
                ReceiptNumber = r.ReceiptNumber,
                Type = r.Type,
                Source = r.IsOnline ? "Online" : "Pos",
                BranchName = r.BranchName,
                CashierName = r.CashierName,
                CustomerName = r.CustomerName,
                ProductId = r.ProductId,
                ProductName = r.ProductName,
                Barcode = r.Barcode,
                Quantity = r.Quantity,
                RefundedQuantity = r.RefundedQuantity,
                UnitPrice = r.UnitPrice,
                DiscountAmount = r.DiscountAmount,
                LineTotal = r.LineTotal,
                CostPrice = r.CostPrice,
                Profit = sign * Math.Round(r.LineTotal - r.CostPrice * r.Quantity, 2),
                PaymentMethod = r.PaymentMethod,
                CashbackEarned = r.SaleLinesTotal > 0 && r.Points != 0
                    ? Math.Round(r.Points * r.LineTotal / r.SaleLinesTotal, 2)
                    : 0,
                FiscalStatus = r.FiscalStatus,
                OnlineOrderNumber = r.OnlineOrderNumber
            };
        }).ToList();
    }
}
