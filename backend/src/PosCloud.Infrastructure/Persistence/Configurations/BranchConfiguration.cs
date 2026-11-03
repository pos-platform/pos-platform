using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(b => b.Name).IsRequired().HasMaxLength(256);
        builder.Property(b => b.Address).HasMaxLength(512);
        builder.Property(b => b.Balance).HasColumnType("numeric(18,2)");
    }
}
