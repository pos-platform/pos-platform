using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(256);
        builder.Property(s => s.Phone).HasMaxLength(32);
        builder.Property(s => s.Address).HasMaxLength(512);
        builder.Property(s => s.Balance).HasColumnType("numeric(18,2)");

        builder.HasIndex(s => s.Name);
    }
}
