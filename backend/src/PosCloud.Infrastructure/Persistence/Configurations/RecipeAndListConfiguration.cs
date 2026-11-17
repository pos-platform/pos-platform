using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

// Bosqich 11.8: retseptlar va oilaviy ro'yxat.

public class ShopRecipeConfiguration : IEntityTypeConfiguration<ShopRecipe>
{
    public void Configure(EntityTypeBuilder<ShopRecipe> builder)
    {
        builder.Property(r => r.Title).IsRequired().HasMaxLength(80);
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.ImageUrl).HasMaxLength(500);
        builder.Property(r => r.Steps).IsRequired().HasMaxLength(8000);
        builder.HasIndex(r => new { r.OrganizationId, r.SortOrder });
        builder.HasOne(r => r.Branch).WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopRecipeItemConfiguration : IEntityTypeConfiguration<ShopRecipeItem>
{
    public void Configure(EntityTypeBuilder<ShopRecipeItem> builder)
    {
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.Note).HasMaxLength(80);
        builder.HasIndex(i => new { i.RecipeId, i.ProductId }).IsUnique();
        builder.HasOne(i => i.Recipe).WithMany(r => r.Items).HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopListConfiguration : IEntityTypeConfiguration<ShopList>
{
    public void Configure(EntityTypeBuilder<ShopList> builder)
    {
        builder.Property(l => l.Name).IsRequired().HasMaxLength(60);
        builder.HasIndex(l => l.OwnerId);
        builder.HasOne(l => l.Owner).WithMany().HasForeignKey(l => l.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopListMemberConfiguration : IEntityTypeConfiguration<ShopListMember>
{
    public void Configure(EntityTypeBuilder<ShopListMember> builder)
    {
        builder.Property(m => m.Phone).IsRequired().HasMaxLength(13);
        builder.HasIndex(m => new { m.ListId, m.Phone }).IsUnique();
        builder.HasIndex(m => m.Phone);
        builder.HasIndex(m => m.ShopUserId);
        builder.HasOne(m => m.List).WithMany(l => l.Members).HasForeignKey(m => m.ListId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.ShopUser).WithMany().HasForeignKey(m => m.ShopUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ShopListItemConfiguration : IEntityTypeConfiguration<ShopListItem>
{
    public void Configure(EntityTypeBuilder<ShopListItem> builder)
    {
        builder.Property(i => i.Title).IsRequired().HasMaxLength(120);
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.CheckedByName).HasMaxLength(100);
        builder.Property(i => i.AddedByName).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.ListId);
        builder.HasOne(i => i.List).WithMany(l => l.Items).HasForeignKey(i => i.ListId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.SetNull);
    }
}
