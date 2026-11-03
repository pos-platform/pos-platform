using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class DebtorConfiguration : IEntityTypeConfiguration<Debtor>
{
    public void Configure(EntityTypeBuilder<Debtor> builder)
    {
        builder.Property(d => d.Name).IsRequired().HasMaxLength(256);
        builder.Property(d => d.Phone).HasMaxLength(32);
        builder.Property(d => d.Comment).HasMaxLength(512);
        builder.Property(d => d.Balance).HasColumnType("numeric(18,2)");

        builder.HasIndex(d => d.Name);

        builder.HasMany(d => d.Transactions)
            .WithOne(t => t.Debtor)
            .HasForeignKey(t => t.DebtorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DebtorTransactionConfiguration : IEntityTypeConfiguration<DebtorTransaction>
{
    public void Configure(EntityTypeBuilder<DebtorTransaction> builder)
    {
        builder.Property(t => t.Amount).HasColumnType("numeric(18,2)");
        builder.Property(t => t.BalanceAfter).HasColumnType("numeric(18,2)");
        builder.Property(t => t.Comment).HasMaxLength(512);
        builder.HasIndex(t => t.CreatedAt);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Comment).HasMaxLength(512);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
