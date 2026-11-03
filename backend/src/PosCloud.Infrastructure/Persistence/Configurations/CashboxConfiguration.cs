using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class CashboxConfiguration : IEntityTypeConfiguration<Cashbox>
{
    public void Configure(EntityTypeBuilder<Cashbox> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Balance).HasColumnType("numeric(18,2)");

        builder.HasIndex(c => new { c.BranchId, c.Name }).IsUnique();

        builder.HasOne(c => c.Branch)
            .WithMany(b => b.Cashboxes)
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
