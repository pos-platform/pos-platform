using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.Property(r => r.FiscalSign).HasMaxLength(256);
        builder.Property(r => r.FiscalModuleId).HasMaxLength(64);
        builder.Property(r => r.FiscalRegistrationNumber).HasMaxLength(64);
        builder.Property(r => r.FiscalReceiptNumber).HasMaxLength(64);
        builder.Property(r => r.QrCodeUrl).HasMaxLength(512);
        builder.Property(r => r.ErrorMessage).HasMaxLength(1024);

        builder.HasIndex(r => r.SaleId).IsUnique();
        builder.HasIndex(r => r.FiscalStatus);
    }
}
