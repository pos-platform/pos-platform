using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Common;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity konfiguratsiyalarini shu assembly'dan avtomatik qo'llaymiz.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // CreatedAt/UpdatedAt ni avtomatik to'ldiramiz.
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
