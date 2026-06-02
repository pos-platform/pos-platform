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
    DbSet<Organization> Organizations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Cashbox> Cashboxes { get; }
    DbSet<User> Users { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<CashShift> CashShifts { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<StockReceipt> StockReceipts { get; }
    DbSet<StockReceiptItem> StockReceiptItems { get; }
    DbSet<Debtor> Debtors { get; }
    DbSet<DebtorTransaction> DebtorTransactions { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<WriteOff> WriteOffs { get; }
    DbSet<WriteOffItem> WriteOffItems { get; }
    DbSet<SupplierPayment> SupplierPayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
