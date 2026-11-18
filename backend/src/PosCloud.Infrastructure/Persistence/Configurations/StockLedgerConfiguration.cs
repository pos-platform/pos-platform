using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Persistence.Configurations;

internal static class ConcurrencyExtensions
{
    /// <summary>
    /// PostgreSQL tizim ustuni <c>xmin</c>'ni optimistik bloklash tokeni qiladi (yangi ustun
    /// yaratilmaydi). Yozuv o'qilgandan keyin boshqa so'rov uni o'zgartirgan bo'lsa,
    /// SaveChanges DbUpdateConcurrencyException tashlaydi.
    /// </summary>
    public static void UseXminConcurrency<T>(this EntityTypeBuilder<T> builder) where T : class
        => builder.Property<uint>("RowVersion").IsRowVersion(); // Npgsql: uint + IsRowVersion → xmin
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(m => m.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(m => m.BalanceAfter).HasColumnType("numeric(18,3)");
        builder.Property(m => m.CostPrice).HasColumnType("numeric(18,2)");
        builder.Property(m => m.DocumentNumber).HasMaxLength(64);

        builder.HasIndex(m => new { m.BranchId, m.CreatedAt });
        builder.HasIndex(m => new { m.ProductId, m.BranchId, m.CreatedAt });
        builder.HasIndex(m => m.DocumentId);

        builder.HasOne(m => m.Branch)
            .WithMany()
            .HasForeignKey(m => m.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.Property(t => t.ClientRequestId).HasMaxLength(64);
        builder.HasIndex(t => t.ClientRequestId)
            .IsUnique()
            .HasFilter("\"ClientRequestId\" IS NOT NULL");

        // Qabul qilish va bekor qilish bir vaqtda bo'lsa — ikkinchisi rad etiladi.
        builder.UseXminConcurrency();
    }
}
