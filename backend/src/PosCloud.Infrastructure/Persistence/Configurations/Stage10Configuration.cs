using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

// Bosqich 10: qaytarish, xatoliklar, keshbek tarixi, kuryer taklifi.

public class OnlineReturnConfiguration : IEntityTypeConfiguration<OnlineReturn>
{
    public void Configure(EntityTypeBuilder<OnlineReturn> builder)
    {
        builder.HasIndex(r => new { r.OrganizationId, r.Number }).IsUnique();
        builder.HasIndex(r => new { r.BranchId, r.Status });
        builder.HasIndex(r => new { r.ShopUserId, r.CreatedAt });
        builder.HasIndex(r => r.OnlineOrderId);
        builder.HasIndex(r => new { r.CourierId, r.Status });

        builder.Property(r => r.Comment).HasMaxLength(500);
        builder.Property(r => r.RejectReason).HasMaxLength(500);
        builder.Property(r => r.CourierName).HasMaxLength(100);
        builder.Property(r => r.CourierPhone).HasMaxLength(20);
        builder.Property(r => r.PhotoUrls).HasDefaultValueSql("'{}'::text[]");
        builder.Property(r => r.RefundAmount).HasColumnType("numeric(18,2)");
        builder.Property(r => r.LoyaltyWithheldAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(r => r.OnlineOrder).WithMany().HasForeignKey(r => r.OnlineOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OnlineReturnItemConfiguration : IEntityTypeConfiguration<OnlineReturnItem>
{
    public void Configure(EntityTypeBuilder<OnlineReturnItem> builder)
    {
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.HasOne(i => i.OnlineReturn).WithMany(r => r.Items).HasForeignKey(i => i.OnlineReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SystemErrorConfiguration : IEntityTypeConfiguration<SystemError>
{
    public void Configure(EntityTypeBuilder<SystemError> builder)
    {
        builder.ToTable("SystemErrors");
        // Bitta fingerprint — bitta qator (hal qilingandan keyin qayta chiqsa — qayta ochiladi).
        builder.HasIndex(e => e.Fingerprint).IsUnique();
        builder.HasIndex(e => e.LastSeenAt);
        builder.HasIndex(e => new { e.ResolvedAt, e.LastSeenAt });

        builder.Property(e => e.Fingerprint).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Details).HasMaxLength(16000);
        builder.Property(e => e.Path).HasMaxLength(300);
        builder.Property(e => e.Method).HasMaxLength(10);
        builder.Property(e => e.OrganizationName).HasMaxLength(200);
        builder.Property(e => e.UserName).HasMaxLength(100);
        builder.Property(e => e.AppVersion).HasMaxLength(50);
        builder.Property(e => e.Device).HasMaxLength(200);
        builder.Property(e => e.RequestId).HasMaxLength(100);
        builder.Property(e => e.ResolvedBy).HasMaxLength(100);
    }
}

public class LoyaltySettingsChangeConfiguration : IEntityTypeConfiguration<LoyaltySettingsChange>
{
    public void Configure(EntityTypeBuilder<LoyaltySettingsChange> builder)
    {
        builder.HasIndex(c => new { c.OrganizationId, c.ChangedAt });
        builder.Property(c => c.Field).IsRequired().HasMaxLength(100);
        builder.Property(c => c.UserName).HasMaxLength(100);
        builder.Property(c => c.OldValue).HasMaxLength(200);
        builder.Property(c => c.NewValue).HasMaxLength(200);
    }
}

public class CourierOfferConfiguration : IEntityTypeConfiguration<CourierOffer>
{
    public void Configure(EntityTypeBuilder<CourierOffer> builder)
    {
        builder.HasIndex(o => new { o.OnlineOrderId, o.CourierUserId });
        builder.HasIndex(o => new { o.CourierUserId, o.Status });
        builder.HasIndex(o => new { o.Status, o.ExpiresAt });
        builder.Property(o => o.DeclineReason).HasMaxLength(300);
        builder.HasOne(o => o.OnlineOrder).WithMany().HasForeignKey(o => o.OnlineOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.CourierUser).WithMany().HasForeignKey(o => o.CourierUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffDeviceTokenConfiguration : IEntityTypeConfiguration<StaffDeviceToken>
{
    public void Configure(EntityTypeBuilder<StaffDeviceToken> builder)
    {
        builder.Property(t => t.Token).IsRequired().HasMaxLength(512);
        builder.HasIndex(t => t.Token).IsUnique();
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(10);
        builder.HasIndex(t => t.UserId);
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
