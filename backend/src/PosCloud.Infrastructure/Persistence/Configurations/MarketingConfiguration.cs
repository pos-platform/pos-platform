using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

// Bosqich 9: mobil ilova marketingi.

public class ShopBannerConfiguration : IEntityTypeConfiguration<ShopBanner>
{
    public void Configure(EntityTypeBuilder<ShopBanner> builder)
    {
        builder.Property(b => b.Title).IsRequired().HasMaxLength(80);
        builder.Property(b => b.Subtitle).HasMaxLength(160);
        builder.Property(b => b.ImageUrl).HasMaxLength(500);
        builder.Property(b => b.BackgroundColor).HasMaxLength(7);
        builder.Property(b => b.LinkValue).HasMaxLength(500);
        builder.HasIndex(b => new { b.OrganizationId, b.SortOrder });
        builder.HasOne(b => b.Branch).WithMany().HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopCollectionConfiguration : IEntityTypeConfiguration<ShopCollection>
{
    public void Configure(EntityTypeBuilder<ShopCollection> builder)
    {
        builder.Property(c => c.Title).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.ImageUrl).HasMaxLength(500);
        builder.HasIndex(c => new { c.OrganizationId, c.SortOrder });
        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopCollectionItemConfiguration : IEntityTypeConfiguration<ShopCollectionItem>
{
    public void Configure(EntityTypeBuilder<ShopCollectionItem> builder)
    {
        builder.HasIndex(i => new { i.CollectionId, i.ProductId }).IsUnique();
        builder.HasOne(i => i.Collection).WithMany(c => c.Items).HasForeignKey(i => i.CollectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopStoryConfiguration : IEntityTypeConfiguration<ShopStory>
{
    public void Configure(EntityTypeBuilder<ShopStory> builder)
    {
        builder.Property(s => s.Title).IsRequired().HasMaxLength(40);
        builder.Property(s => s.CoverImageUrl).IsRequired().HasMaxLength(500);
        builder.HasIndex(s => new { s.OrganizationId, s.SortOrder });
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopStorySlideConfiguration : IEntityTypeConfiguration<ShopStorySlide>
{
    public void Configure(EntityTypeBuilder<ShopStorySlide> builder)
    {
        builder.Property(s => s.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.Caption).HasMaxLength(200);
        builder.Property(s => s.LinkValue).HasMaxLength(500);
        builder.HasIndex(s => s.StoryId);
        builder.HasOne(s => s.Story).WithMany(s => s.Slides).HasForeignKey(s => s.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopFavoriteConfiguration : IEntityTypeConfiguration<ShopFavorite>
{
    public void Configure(EntityTypeBuilder<ShopFavorite> builder)
    {
        builder.HasIndex(f => new { f.ShopUserId, f.ProductId }).IsUnique();
        builder.HasOne(f => f.ShopUser).WithMany().HasForeignKey(f => f.ShopUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Product).WithMany().HasForeignKey(f => f.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopStockSubscriptionConfiguration : IEntityTypeConfiguration<ShopStockSubscription>
{
    public void Configure(EntityTypeBuilder<ShopStockSubscription> builder)
    {
        builder.HasIndex(s => new { s.ShopUserId, s.ProductId, s.BranchId }).IsUnique();
        builder.HasIndex(s => s.NotifiedAt);
        builder.HasOne(s => s.ShopUser).WithMany().HasForeignKey(s => s.ShopUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopCartSnapshotConfiguration : IEntityTypeConfiguration<ShopCartSnapshot>
{
    public void Configure(EntityTypeBuilder<ShopCartSnapshot> builder)
    {
        builder.HasIndex(s => new { s.ShopUserId, s.BranchId }).IsUnique();
        builder.HasIndex(s => s.ChangedAt);
        builder.Property(s => s.ItemsJson).IsRequired();
        builder.HasOne(s => s.ShopUser).WithMany().HasForeignKey(s => s.ShopUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopReferralConfiguration : IEntityTypeConfiguration<ShopReferral>
{
    public void Configure(EntityTypeBuilder<ShopReferral> builder)
    {
        builder.HasIndex(r => new { r.OrganizationId, r.RefereeShopUserId }).IsUnique();
        builder.HasIndex(r => new { r.OrganizationId, r.ReferrerShopUserId });
        builder.Property(r => r.ReferrerBonus).HasColumnType("numeric(18,2)");
        builder.Property(r => r.RefereeBonus).HasColumnType("numeric(18,2)");
        builder.HasOne<ShopUser>().WithMany().HasForeignKey(r => r.ReferrerShopUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ShopUser>().WithMany().HasForeignKey(r => r.RefereeShopUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShopPushCampaignConfiguration : IEntityTypeConfiguration<ShopPushCampaign>
{
    public void Configure(EntityTypeBuilder<ShopPushCampaign> builder)
    {
        builder.Property(c => c.Title).IsRequired().HasMaxLength(60);
        builder.Property(c => c.Body).IsRequired().HasMaxLength(200);
        builder.Property(c => c.LinkValue).HasMaxLength(500);
        builder.HasIndex(c => new { c.Status, c.ScheduledAt });
        builder.HasIndex(c => new { c.OrganizationId, c.CreatedAt });
        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LoyaltyTierConfiguration : IEntityTypeConfiguration<LoyaltyTier>
{
    public void Configure(EntityTypeBuilder<LoyaltyTier> builder)
    {
        builder.ToTable("LoyaltyTiers");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(30);
        builder.Property(t => t.MinSpend).HasColumnType("numeric(18,2)");
        builder.Property(t => t.CashbackPercent).HasColumnType("numeric(5,2)");
        builder.HasIndex(t => t.OrganizationId);
    }
}
