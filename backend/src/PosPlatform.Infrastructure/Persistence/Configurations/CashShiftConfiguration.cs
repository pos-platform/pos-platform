using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
{
    public void Configure(EntityTypeBuilder<CashShift> builder)
    {
        builder.Property(s => s.OpeningCash).HasColumnType("numeric(18,2)");
        builder.Property(s => s.CountedCash).HasColumnType("numeric(18,2)");
        builder.Property(s => s.TotalSales).HasColumnType("numeric(18,2)");
        builder.Property(s => s.TotalRefunds).HasColumnType("numeric(18,2)");
        builder.Property(s => s.CashSalesNet).HasColumnType("numeric(18,2)");
        builder.Property(s => s.CardSalesNet).HasColumnType("numeric(18,2)");
        builder.Property(s => s.ExpectedCash).HasColumnType("numeric(18,2)");
        builder.Property(s => s.CashDifference).HasColumnType("numeric(18,2)");

        builder.Property(s => s.TerminalId).HasMaxLength(64);

        builder.HasIndex(s => new { s.CashierId, s.Status });

        builder.HasOne(s => s.Cashier)
            .WithMany()
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Sales)
            .WithOne(sale => sale.CashShift)
            .HasForeignKey(sale => sale.CashShiftId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
