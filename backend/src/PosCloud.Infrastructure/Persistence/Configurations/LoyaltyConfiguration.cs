using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class LoyaltySettingsConfiguration : IEntityTypeConfiguration<LoyaltySettings>
{
    public void Configure(EntityTypeBuilder<LoyaltySettings> builder)
    {
        builder.ToTable("LoyaltySettings");
        builder.HasIndex(s => s.OrganizationId).IsUnique();
        builder.Property(s => s.CashbackPercent).HasColumnType("numeric(5,2)");
        builder.Property(s => s.MaxRedeemPercent).HasColumnType("numeric(5,2)");
        builder.Property(s => s.MinSaleAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.SignupBonus).HasColumnType("numeric(18,2)");
    }
}

public class LoyaltyCategoryRateConfiguration : IEntityTypeConfiguration<LoyaltyCategoryRate>
{
    public void Configure(EntityTypeBuilder<LoyaltyCategoryRate> builder)
    {
        builder.ToTable("LoyaltyCategoryRates");
        builder.HasIndex(r => new { r.OrganizationId, r.CategoryId }).IsUnique();
        builder.Property(r => r.Percent).HasColumnType("numeric(5,2)");
        builder.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        // Tarix va hisobot so'rovlari uchun.
        builder.HasIndex(t => new { t.CustomerId, t.CreatedAt });
        builder.HasIndex(t => new { t.OrganizationId, t.CreatedAt });
        builder.Property(t => t.BalanceAfter).HasColumnType("numeric(18,2)");
        builder.Property(t => t.UserName).HasMaxLength(100);
    }
}
