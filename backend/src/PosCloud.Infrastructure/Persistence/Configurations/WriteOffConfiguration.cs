using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class WriteOffConfiguration : IEntityTypeConfiguration<WriteOff>
{
    public void Configure(EntityTypeBuilder<WriteOff> builder)
    {
        builder.Property(w => w.Number).IsRequired().HasMaxLength(32);
        builder.HasIndex(w => w.Number).IsUnique();
        builder.HasIndex(w => w.CreatedAt);
        builder.Property(w => w.Reason).HasMaxLength(512);
        builder.Property(w => w.TotalCost).HasColumnType("numeric(18,2)");

        builder.Property(w => w.ClientRequestId).HasMaxLength(64);
        builder.HasIndex(w => w.ClientRequestId)
            .IsUnique()
            .HasFilter("\"ClientRequestId\" IS NOT NULL");
        builder.UseXminConcurrency(); // parallel ikki marta bekor qilishdan himoya

        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(w => w.Items)
            .WithOne(i => i.WriteOff)
            .HasForeignKey(i => i.WriteOffId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WriteOffItemConfiguration : IEntityTypeConfiguration<WriteOffItem>
{
    public void Configure(EntityTypeBuilder<WriteOffItem> builder)
    {
        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.CostPrice).HasColumnType("numeric(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Comment).HasMaxLength(512);
        builder.HasIndex(p => p.CreatedAt);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
