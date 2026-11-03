using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.MxikCode).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.Property(p => p.CostPrice).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Price).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Tare).HasColumnType("numeric(18,3)");
        builder.Property(p => p.ImageUrl).HasMaxLength(512);
        builder.Property(p => p.Description).HasMaxLength(1024);
        builder.Property(p => p.MarkingCode).HasMaxLength(128);
        builder.Property(p => p.PackageCode).HasMaxLength(128);
        builder.Property(p => p.CommitentTin).HasMaxLength(14);

        builder.HasIndex(p => p.Barcode);
        builder.HasIndex(p => p.PluNumber);

        // GET /api/products har doim tashkilot filtri bilan ketadi va Name bo'yicha saralanadi.
        // (OrganizationId, Name) kompozit index — filtr + ORDER BY ni to'liq qoplaydi.
        builder.HasIndex(p => new { p.OrganizationId, p.Name });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
