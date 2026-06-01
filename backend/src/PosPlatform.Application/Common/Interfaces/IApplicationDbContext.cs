using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Common.Interfaces;

/// <summary>
/// DbContext'ning Application qatlamiga ochiq abstraksiyasi.
/// Infrastructure (EF Core) bunga bog'liq, Application esa konkret
/// DbContext'ni bilmaydi — shu sabab qatlamlar bog'liqligi toza qoladi.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Branch> Branches { get; }
    DbSet<User> Users { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<CashShift> CashShifts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
