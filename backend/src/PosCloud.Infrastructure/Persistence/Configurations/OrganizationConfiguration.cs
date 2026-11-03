using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Phone).HasMaxLength(40);
        builder.Property(o => o.Note).HasMaxLength(500);

        builder.HasMany(o => o.Branches)
            .WithOne(b => b.Organization)
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
