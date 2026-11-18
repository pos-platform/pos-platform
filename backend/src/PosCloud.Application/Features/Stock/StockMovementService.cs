using PosCloud.Application.Common;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stock.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Stock;

public interface IStockMovementService
{
    /// <summary>Tovar aylanmasi: har tovar bo'yicha boshlang'ich, kirim/chiqim turlari va oxirgi qoldiq.</summary>
    Task<StockTurnoverReportDto> GetTurnoverAsync(StockTurnoverQuery query, CancellationToken ct = default);

    /// <summary>Bitta tovar qoldig'i tarixi (eng yangisi birinchi).</summary>
    Task<IReadOnlyList<StockMovementDto>> GetProductMovementsAsync(Guid productId, Guid? branchId,
        DateTime? from, DateTime? to, CancellationToken ct = default);
}

public class StockMovementService : IStockMovementService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StockMovementService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StockTurnoverReportDto> GetTurnoverAsync(StockTurnoverQuery query, CancellationToken ct = default)
    {
        var (fromDay, toDay, fromUtc, toUtc) = Range(query.From, query.To);
        // Filialga biriktirilgan xodim faqat o'z filialini ko'radi.
        var branchId = _currentUser.BranchId ?? query.BranchId;

        var movements = _db.StockMovements.AsNoTracking();
        var stocks = _db.BranchStocks.AsNoTracking();
        if (branchId is { } b)
        {
            movements = movements.Where(m => m.BranchId == b);
            stocks = stocks.Where(s => s.BranchId == b);
        }
        if (query.ProductId is { } pid)
        {
            movements = movements.Where(m => m.ProductId == pid);
            stocks = stocks.Where(s => s.ProductId == pid);
        }

        // Davr ichidagi harakatlar (tovar + tur bo'yicha yig'indi)
        var inRange = await movements
            .Where(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc)
            .GroupBy(m => new { m.ProductId, m.Type })
            .Select(g => new { g.Key.ProductId, g.Key.Type, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        // Davrdan keyingi harakatlar — oxirgi qoldiqni hozirgi qoldiqdan orqaga hisoblash uchun.
        // (Jurnal joriy etilishidan oldingi tarix bo'lmasa ham to'g'ri ishlaydi.)
        var afterRange = await movements
            .Where(m => m.CreatedAt >= toUtc)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var current = await stocks
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var moved = inRange.Select(x => x.ProductId).ToHashSet();
        var productIds = (query.IncludeIdle
            ? moved.Union(current.Where(c => c.Value != 0).Select(c => c.Key))
            : moved).ToList();

        var productsQuery = _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(s) || (p.Barcode != null && p.Barcode.Contains(s)));
        }
        var products = await productsQuery
            .Select(p => new { p.Id, p.Name, p.Barcode, p.Unit })
            .ToListAsync(ct);

        var byProduct = inRange.ToLookup(x => x.ProductId);
        var rows = new List<StockTurnoverRowDto>(products.Count);
        foreach (var p in products.OrderBy(p => p.Name))
        {
            var row = new StockTurnoverRowDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Barcode = p.Barcode,
                Unit = p.Unit.ToString()
            };
            decimal net = 0;
            foreach (var m in byProduct[p.Id])
            {
                net += m.Qty;
                switch (m.Type)
                {
                    case StockMovementType.Receipt: row.Receipt += m.Qty; break;
                    case StockMovementType.Refund: row.Refund += m.Qty; break;
                    case StockMovementType.TransferIn: row.TransferIn += m.Qty; break;
                    case StockMovementType.Sale: row.Sale -= m.Qty; break;
                    case StockMovementType.WriteOff: row.WriteOff -= m.Qty; break;
                    case StockMovementType.TransferOut: row.TransferOut -= m.Qty; break;
                    case StockMovementType.Inventory: row.Inventory += m.Qty; break;
                    case StockMovementType.ReceiptCancel: row.OtherOut -= m.Qty; break;
                    default:
                        // Initial, WriteOffCancel, TransferCancel, OnlineReturn — boshqa kirimlar
                        if (m.Qty >= 0) row.OtherIn += m.Qty; else row.OtherOut -= m.Qty;
                        break;
                }
            }

            row.Closing = (current.TryGetValue(p.Id, out var cur) ? cur : 0m)
                          - (afterRange.TryGetValue(p.Id, out var after) ? after : 0m);
            row.Opening = row.Closing - net;
            row.TotalIn = row.Receipt + row.Refund + row.TransferIn + row.OtherIn + Math.Max(0, row.Inventory);
            row.TotalOut = row.Sale + row.WriteOff + row.TransferOut + row.OtherOut + Math.Max(0, -row.Inventory);
            rows.Add(row);
        }

        return new StockTurnoverReportDto { From = fromDay, To = toDay, BranchId = branchId, Rows = rows };
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetProductMovementsAsync(Guid productId, Guid? branchId,
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new NotFoundException($"Mahsulot topilmadi: {productId}");

        var q = _db.StockMovements.AsNoTracking()
            .Include(m => m.Branch)
            .Where(m => m.ProductId == productId);

        var effectiveBranch = _currentUser.BranchId ?? branchId;
        if (effectiveBranch is { } b) q = q.Where(m => m.BranchId == b);
        if (from.HasValue || to.HasValue)
        {
            var (_, _, fromUtc, toUtc) = Range(from, to);
            q = q.Where(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc);
        }

        var list = await q.OrderByDescending(m => m.CreatedAt).Take(1000).ToListAsync(ct);

        var userIds = list.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).Distinct().ToList();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return list.Select(m => StockMovementDto.FromEntity(m,
            m.UserId is { } uid && users.TryGetValue(uid, out var n) ? n : null)).ToList();
    }

    /// <summary>Mahalliy kunlar oralig'ini [fromUtc, toUtc) UTC chegarasiga aylantiradi (standart: joriy oy).</summary>
    private static (DateTime FromDay, DateTime ToDay, DateTime FromUtc, DateTime ToUtc) Range(DateTime? from, DateTime? to)
    {
        var today = BusinessTime.TodayLocal;
        var fromDay = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var toDay = (to ?? today).Date;
        if (toDay < fromDay) throw new ValidationException("Sana oralig'i noto'g'ri: 'gacha' sanasi 'dan' sanasidan oldin.");

        var (fromUtc, toUtc) = BusinessTime.LocalRangeToUtc(fromDay, toDay);
        return (fromDay, toDay, fromUtc, toUtc);
    }
}
