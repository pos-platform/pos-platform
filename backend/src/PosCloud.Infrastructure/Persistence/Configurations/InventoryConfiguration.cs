using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.Property(i => i.Number).IsRequired().HasMaxLength(32);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.CreatedAt);
        builder.Property(i => i.Notes).HasMaxLength(512);

        builder.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Items)
            .WithOne(item => item.Inventory)
            .HasForeignKey(item => item.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.Property(i => i.ExpectedQty).HasColumnType("numeric(18,3)");
        builder.Property(i => i.ActualQty).HasColumnType("numeric(18,3)");

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
