using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

// Mobil ilova (online do'kon) entity konfiguratsiyalari.

public class ShopUserConfiguration : IEntityTypeConfiguration<ShopUser>
{
    public void Configure(EntityTypeBuilder<ShopUser> builder)
    {
        builder.Property(u => u.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(u => u.Phone).IsUnique();
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.ReferralCode).HasMaxLength(8);
        builder.HasIndex(u => u.ReferralCode).IsUnique().HasFilter("\"ReferralCode\" IS NOT NULL");
    }
}

public class ShopUserAddressConfiguration : IEntityTypeConfiguration<ShopUserAddress>
{
    public void Configure(EntityTypeBuilder<ShopUserAddress> builder)
    {
        builder.Property(a => a.Label).IsRequired().HasMaxLength(50);
        builder.Property(a => a.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Entrance).HasMaxLength(20);
        builder.Property(a => a.Floor).HasMaxLength(20);
        builder.Property(a => a.Apartment).HasMaxLength(20);
        builder.Property(a => a.Intercom).HasMaxLength(50);
        builder.Property(a => a.Landmark).HasMaxLength(200);
        builder.Property(a => a.Comment).HasMaxLength(500);

        builder.HasOne(a => a.ShopUser)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.ShopUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShopOtpCodeConfiguration : IEntityTypeConfiguration<ShopOtpCode>
{
    public void Configure(EntityTypeBuilder<ShopOtpCode> builder)
    {
        builder.Property(o => o.Phone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(128);
        builder.Property(o => o.Channel).IsRequired().HasMaxLength(20);
        builder.Property(o => o.IpAddress).HasMaxLength(64);
        builder.HasIndex(o => new { o.Phone, o.CreatedAt });
        builder.HasIndex(o => o.ExpiresAt);
    }
}

public class ShopRefreshTokenConfiguration: IEntityTypeConfiguration<ShopRefreshToken>
{
    public void Configure(EntityTypeBuilder<ShopRefreshToken> builder)
    {
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.ExpiresAt);

        builder.HasOne(t => t.ShopUser)
            .WithMany()
            .HasForeignKey(t => t.ShopUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OnlineOrderConfiguration : IEntityTypeConfiguration<OnlineOrder>
{
    public void Configure(EntityTypeBuilder<OnlineOrder> builder)
    {
        builder.HasIndex(o => new { o.OrganizationId, o.Number }).IsUnique();
        builder.HasIndex(o => new { o.BranchId, o.Status });
        builder.HasIndex(o => new { o.ShopUserId, o.CreatedAt });

        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CustomerPhone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.AddressLine).HasMaxLength(300);
        builder.Property(o => o.Entrance).HasMaxLength(20);
        builder.Property(o => o.Floor).HasMaxLength(20);
        builder.Property(o => o.Apartment).HasMaxLength(20);
        builder.Property(o => o.Intercom).HasMaxLength(50);
        builder.Property(o => o.Landmark).HasMaxLength(200);
        builder.Property(o => o.Comment).HasMaxLength(500);
        builder.Property(o => o.CancelReason).HasMaxLength(500);
        builder.Property(o => o.TransferProofUrl).HasMaxLength(500);
        builder.Property(o => o.CourierName).HasMaxLength(100);
        builder.Property(o => o.CourierPhone).HasMaxLength(20);
        builder.Property(o => o.ClientRequestId).HasMaxLength(64);

        // Idempotentlik: bir mijozda bir xil kalit bilan faqat bitta buyurtma.
        builder.HasIndex(o => new { o.ShopUserId, o.ClientRequestId })
            .IsUnique()
            .HasFilter("\"ClientRequestId\" IS NOT NULL");

        builder.Property(o => o.ItemsTotal).HasColumnType("numeric(18,2)");
        builder.Property(o => o.DeliveryFee).HasColumnType("numeric(18,2)");
        builder.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(o => o.Branch).WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ShopUser).WithMany().HasForeignKey(o => o.ShopUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.Sale).WithMany().HasForeignKey(o => o.SaleId).OnDelete(DeleteBehavior.SetNull);

        // Bosqich 6: kuryer, promokod, bonus, online to'lov.
        builder.Property(o => o.CourierCashCollected).HasColumnType("numeric(18,2)");
        builder.Property(o => o.PromoCode).HasMaxLength(50);
        builder.Property(o => o.PromoDiscount).HasColumnType("numeric(18,2)");
        builder.Property(o => o.BonusUsed).HasColumnType("numeric(18,2)");
        builder.HasIndex(o => new { o.CourierUserId, o.Status });
        builder.HasIndex(o => new { o.IsPreorder, o.Status, o.ScheduledFor });
        // Kuryer taklifini atomik qabul qilish: UPDATE ... WHERE CourierUserId = (asl qiymat) — ikkinchisi 409.
        builder.Property(o => o.CourierUserId).IsConcurrencyToken();
        builder.HasIndex(o => o.PromoCodeId);

        // Bosqich 9: mijoz bahosi.
        builder.Property(o => o.RatingComment).HasMaxLength(500);
        builder.Property(o => o.RatingTags).HasMaxLength(200);
        builder.HasIndex(o => new { o.OrganizationId, o.RatedAt });

        builder.HasOne(o => o.CourierUser).WithMany().HasForeignKey(o => o.CourierUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PromoCode>().WithMany().HasForeignKey(o => o.PromoCodeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => new { p.OrganizationId, p.Code }).IsUnique();
        builder.Property(p => p.DiscountPercent).HasColumnType("numeric(5,2)");
        builder.Property(p => p.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.MinOrderAmount).HasColumnType("numeric(18,2)");
    }
}

public class OnlineOrderItemConfiguration : IEntityTypeConfiguration<OnlineOrderItem>
{
    public void Configure(EntityTypeBuilder<OnlineOrderItem> builder)
    {
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.PickedQuantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");
        // Mavjud qatorlar uchun bo'sh massiv (NOT NULL ustun).
        builder.Property(i => i.MarkingCodes).HasDefaultValueSql("'{}'::text[]");
        builder.Property(i => i.RejectedQuantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.ReturnedQuantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.RejectReason).HasMaxLength(200);

        builder.HasOne(i => i.OnlineOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OnlineOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OnlineStoreSettingsConfiguration : IEntityTypeConfiguration<OnlineStoreSettings>
{
    public void Configure(EntityTypeBuilder<OnlineStoreSettings> builder)
    {
        builder.ToTable("OnlineStoreSettings");
        builder.HasIndex(s => s.OrganizationId).IsUnique();

        builder.Property(s => s.BlockReason).HasMaxLength(500);
        builder.Property(s => s.DisplayName).HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.TransferCardNumber).HasMaxLength(32);
        builder.Property(s => s.TransferCardHolder).HasMaxLength(200);

        builder.Property(s => s.MinOrderAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.DeliveryFee).HasColumnType("numeric(18,2)");
        builder.Property(s => s.FreeDeliveryFrom).HasColumnType("numeric(18,2)");
        builder.Property(s => s.DeliveryServiceMxikCode).HasMaxLength(20);

        // White-label ilova: slug va paket ID — unik (null'lar hisobga olinmaydi).
        builder.Property(s => s.Slug).HasMaxLength(40);
        builder.HasIndex(s => s.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
        builder.Property(s => s.AppName).HasMaxLength(30);
        builder.Property(s => s.AppPackageId).HasMaxLength(150);
        builder.HasIndex(s => s.AppPackageId).IsUnique().HasFilter("\"AppPackageId\" IS NOT NULL");
        builder.Property(s => s.BrandColor).HasMaxLength(7);
        builder.Property(s => s.AndroidLatestVersion).HasMaxLength(30);
        builder.Property(s => s.AndroidStoreUrl).HasMaxLength(500);
        builder.Property(s => s.IosLatestVersion).HasMaxLength(30);
        builder.Property(s => s.IosStoreUrl).HasMaxLength(500);

        // Bosqich 9: yordam va do'stni taklif.
        builder.Property(s => s.SupportPhone).HasMaxLength(20);
        builder.Property(s => s.SupportTelegram).HasMaxLength(32);
        builder.Property(s => s.ReferrerBonus).HasColumnType("numeric(18,2)");
        builder.Property(s => s.RefereeBonus).HasColumnType("numeric(18,2)");
    }
}

public class ShopDeviceTokenConfiguration : IEntityTypeConfiguration<ShopDeviceToken>
{
    public void Configure(EntityTypeBuilder<ShopDeviceToken> builder)
    {
        builder.Property(t => t.Token).IsRequired().HasMaxLength(512);
        builder.HasIndex(t => t.Token).IsUnique();
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Locale).IsRequired().HasMaxLength(5);
        builder.HasIndex(t => t.ShopUserId);
        builder.HasIndex(t => t.LastSeenAt);

        builder.HasOne(t => t.ShopUser)
            .WithMany()
            .HasForeignKey(t => t.ShopUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
