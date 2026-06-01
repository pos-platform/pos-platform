using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.ReceiptNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        builder.HasIndex(s => s.CreatedAt);

        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.VatAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.PaidAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.ChangeAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Cashier)
            .WithMany()
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Receipt)
            .WithOne(r => r.Sale)
            .HasForeignKey<Receipt>(r => r.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Refund — asl savdoga self-reference (teskari navigatsiyasiz).
        builder.HasOne(s => s.OriginalSale)
            .WithMany()
            .HasForeignKey(s => s.OriginalSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OriginalSaleId);
        builder.HasIndex(s => s.Type);
    }
}
