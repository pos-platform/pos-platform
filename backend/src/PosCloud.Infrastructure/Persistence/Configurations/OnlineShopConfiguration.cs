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

        builder.Property(o => o.ItemsTotal).HasColumnType("numeric(18,2)");
        builder.Property(o => o.DeliveryFee).HasColumnType("numeric(18,2)");
        builder.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(o => o.Branch).WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ShopUser).WithMany().HasForeignKey(o => o.ShopUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.Sale).WithMany().HasForeignKey(o => o.SaleId).OnDelete(DeleteBehavior.SetNull);
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
    }
}
