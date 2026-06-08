using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

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
