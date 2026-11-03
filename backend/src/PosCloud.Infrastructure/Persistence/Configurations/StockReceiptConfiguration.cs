using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.Property(r => r.Number).IsRequired().HasMaxLength(32);
        builder.HasIndex(r => r.Number).IsUnique();
        builder.HasIndex(r => r.CreatedAt);

        builder.Property(r => r.Comment).HasMaxLength(512);
        builder.Property(r => r.TotalAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(r => r.Supplier)
            .WithMany()
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.StockReceipt)
            .HasForeignKey(i => i.StockReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockReceiptItemConfiguration : IEntityTypeConfiguration<StockReceiptItem>
{
    public void Configure(EntityTypeBuilder<StockReceiptItem> builder)
    {
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.CostPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.RemainingQuantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");

        // FIFO tannarx qatlamlarini topish uchun: mahsulot + qolgan miqdor.
        builder.HasIndex(i => new { i.ProductId, i.RemainingQuantity });

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
