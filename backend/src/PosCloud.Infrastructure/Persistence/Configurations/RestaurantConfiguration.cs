using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class DishIngredientConfiguration : IEntityTypeConfiguration<DishIngredient>
{
    public void Configure(EntityTypeBuilder<DishIngredient> builder)
    {
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");

        builder.HasOne(i => i.DishProduct)
            .WithMany(p => p.Ingredients)
            .HasForeignKey(i => i.DishProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.IngredientProduct)
            .WithMany()
            .HasForeignKey(i => i.IngredientProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.DishProductId);
    }
}

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Hall).HasMaxLength(128);

        builder.HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.BranchId);
    }
}

public class KitchenIssueConfiguration : IEntityTypeConfiguration<KitchenIssue>
{
    public void Configure(EntityTypeBuilder<KitchenIssue> builder)
    {
        builder.Property(k => k.Number).IsRequired().HasMaxLength(32);
        builder.HasIndex(k => k.Number).IsUnique();
        builder.HasIndex(k => k.CreatedAt);
        builder.Property(k => k.Comment).HasMaxLength(512);
        builder.Property(k => k.TotalCost).HasColumnType("numeric(18,2)");

        builder.HasOne(k => k.Branch)
            .WithMany()
            .HasForeignKey(k => k.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(k => k.ChefUser)
            .WithMany()
            .HasForeignKey(k => k.ChefUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(k => k.Items)
            .WithOne(i => i.KitchenIssue)
            .HasForeignKey(i => i.KitchenIssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KitchenIssueItemConfiguration : IEntityTypeConfiguration<KitchenIssueItem>
{
    public void Configure(EntityTypeBuilder<KitchenIssueItem> builder)
    {
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.CostPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");

        builder.HasOne(i => i.DishProduct)
            .WithMany()
            .HasForeignKey(i => i.DishProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.IngredientProduct)
            .WithMany()
            .HasForeignKey(i => i.IngredientProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        builder.Property(t => t.TableName).HasMaxLength(64);
        builder.Property(t => t.WaiterName).HasMaxLength(128);
        builder.HasIndex(t => t.Printed);

        builder.HasOne(t => t.Sale)
            .WithMany()
            .HasForeignKey(t => t.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.KitchenTicket)
            .HasForeignKey(i => i.KitchenTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KitchenTicketItemConfiguration : IEntityTypeConfiguration<KitchenTicketItem>
{
    public void Configure(EntityTypeBuilder<KitchenTicketItem> builder)
    {
        builder.Property(i => i.DishName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.Note).HasMaxLength(512);
    }
}
