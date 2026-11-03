using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasMany(r => r.Users)
            .WithOne(u => u.AppRole)
            .HasForeignKey(u => u.AppRoleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
