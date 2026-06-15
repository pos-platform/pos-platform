using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.MxikCode).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Barcode).HasMaxLength(64);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.VatPercent).HasColumnType("numeric(5,2)");
        builder.Property(i => i.VatAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");
        builder.Property(i => i.Note).HasMaxLength(512);

        // Mahsulot o'chmasligi uchun (savdo tarixi saqlanadi) — Restrict.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
