using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.MxikCode).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.Property(p => p.Price).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Tare).HasColumnType("numeric(18,3)");

        builder.HasIndex(p => p.Barcode);
        builder.HasIndex(p => p.PluNumber);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
