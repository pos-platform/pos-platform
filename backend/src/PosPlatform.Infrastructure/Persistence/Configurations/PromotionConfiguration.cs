using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Ignore(p => p.AppliesToAll);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.DiscountPercent).HasColumnType("numeric(5,2)");

        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => new { p.StartDate, p.EndDate });

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
