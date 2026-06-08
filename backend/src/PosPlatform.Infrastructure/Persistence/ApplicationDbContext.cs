using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Common;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Joriy foydalanuvchi tashkiloti — global query filter va auto-set uchun. null = filtrlanmaydi (SuperAdmin/seed/login).</summary>
    private readonly Guid? _organizationId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _organizationId = currentUser.OrganizationId;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Cashbox> Cashboxes => Set<Cashbox>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptItem> StockReceiptItems => Set<StockReceiptItem>();
    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<DebtorTransaction> DebtorTransactions => Set<DebtorTransaction>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<WriteOff> WriteOffs => Set<WriteOff>();
    public DbSet<WriteOffItem> WriteOffItems => Set<WriteOffItem>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<OnlinePayment> OnlinePayments => Set<OnlinePayment>();
    public DbSet<LabelPrintJob> LabelPrintJobs => Set<LabelPrintJob>();
    public DbSet<LabelPrintJobItem> LabelPrintJobItems => Set<LabelPrintJobItem>();
    public DbSet<RoleTemplate> RoleTemplates => Set<RoleTemplate>();

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(ApplicationDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity konfiguratsiyalarini shu assembly'dan avtomatik qo'llaymiz.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Organization — ijarachilik ildizi; uning o'zida OrganizationId ustuni bo'lmaydi.
        modelBuilder.Entity<Organization>().Ignore(o => o.OrganizationId);


        // Ko'p-ijarachilik: Organization va AuditLog'dan tashqari barcha entity'ga
        // tashkilot bo'yicha global query filter qo'yamiz. _organizationId null bo'lsa
        // (SuperAdmin/seed/login) filtr o'chadi — barcha yozuvlar ko'rinadi.
        // AuditLog — tizim/xavfsizlik jurnali, filtrdan ozod (faqat SuperAdmin controller orqali ochiladi).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (clr == typeof(Organization) || clr == typeof(AuditLog)) continue;
            if (!typeof(BaseEntity).IsAssignableFrom(clr)) continue;

            SetTenantFilterMethod.MakeGenericMethod(clr).Invoke(this, new object[] { modelBuilder });
        }

        base.OnModelCreating(modelBuilder);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // Yangi yozuvni joriy tashkilotga biriktiramiz (Organization'ning o'zidan tashqari).
                if (entry.Entity is not Organization
                    && entry.Entity.OrganizationId == null
                    && _organizationId is not null)
                {
                    entry.Entity.OrganizationId = _organizationId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
