using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.ReceiptNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        builder.HasIndex(s => s.CreatedAt);

        // Idempotentlik kaliti: bir xil kalit bilan ikkinchi savdo yaratilmasligi uchun
        // unique indeks. Kalit yuborilmagan (null) savdolarga cheklov qo'llanmaydi.
        builder.Property(s => s.ClientSaleId).HasMaxLength(64);
        builder.HasIndex(s => s.ClientSaleId)
            .IsUnique()
            .HasFilter("\"ClientSaleId\" IS NOT NULL");

        builder.Property(s => s.TerminalId).HasMaxLength(64);
        // Fiskal agent navbatni terminal bo'yicha filtrlaydi (per-terminal fiskal modul).
        builder.HasIndex(s => s.TerminalId);

        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.VatAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.PaidAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.ChangeAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.CardAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.ServiceChargePercent).HasColumnType("numeric(9,2)");
        builder.Property(s => s.ServiceChargeAmount).HasColumnType("numeric(18,2)");

        // Restoran: ochiq buyurtma — stol va ofitsiant.
        builder.HasOne(s => s.Table)
            .WithMany()
            .HasForeignKey(s => s.TableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Waiter)
            .WithMany()
            .HasForeignKey(s => s.WaiterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.Status);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Cashier)
            .WithMany()
            .HasForeignKey(s => s.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Receipt)
            .WithOne(r => r.Sale)
            .HasForeignKey<Receipt>(r => r.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Refund — asl savdoga self-reference (teskari navigatsiyasiz).
        builder.HasOne(s => s.OriginalSale)
            .WithMany()
            .HasForeignKey(s => s.OriginalSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nasiya — qarzdorga bog'lanish.
        builder.HasOne(s => s.Debtor)
            .WithMany()
            .HasForeignKey(s => s.DebtorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.OriginalSaleId);
        builder.HasIndex(s => s.Type);
    }
}
