using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class BranchStockConfiguration : IEntityTypeConfiguration<BranchStock>
{
    public void Configure(EntityTypeBuilder<BranchStock> builder)
    {
        builder.Property(s => s.Quantity).HasColumnType("numeric(18,3)");

        // Bir filial + bir mahsulot = bitta yozuv (tashkilot bo'yicha)
        builder.HasIndex(s => new { s.BranchId, s.ProductId, s.OrganizationId }).IsUnique();

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Stocks)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
