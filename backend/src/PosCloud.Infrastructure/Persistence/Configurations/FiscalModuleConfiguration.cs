using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class FiscalModuleConfiguration : IEntityTypeConfiguration<FiscalModule>
{
    public void Configure(EntityTypeBuilder<FiscalModule> builder)
    {
        builder.Property(m => m.ModuleId).IsRequired().HasMaxLength(64);
        builder.Property(m => m.OwnerName).HasMaxLength(160);
        builder.Property(m => m.Inn).HasMaxLength(32);
        builder.Property(m => m.RegistrationNumber).HasMaxLength(64);
        builder.Property(m => m.TerminalId).HasMaxLength(64);
        builder.Property(m => m.Note).HasMaxLength(512);

        // Bitta tashkilotda bir modul ID faqat bir marta.
        builder.HasIndex(m => new { m.OrganizationId, m.ModuleId }).IsUnique();
    }
}
