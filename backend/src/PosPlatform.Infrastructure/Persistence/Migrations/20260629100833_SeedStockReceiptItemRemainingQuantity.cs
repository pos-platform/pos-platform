using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStockReceiptItemRemainingQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bir martalik seed: mavjud qoldiqni FIFO tannarx qatlamlariga taqsimlaymiz.
            // FIFO bo'yicha eski tovar avval sotilgan → qo'lda qolgan qoldiq eng YANGI
            // kirim partiyalaridan. Har (filial, mahsulot) uchun joriy BranchStock.Quantity ni
            // eng yangi partiyadan boshlab to'ldiramiz; eskilarga 0 qoladi.
            //   cum      = shu qatlamdan (o'zini qo'shib) yangiroq qatlamlar jami miqdori
            //   cum - qty= shu qatlamdan yangiroq qatlamlar jami (o'zisiz)
            //   remaining= clamp(onhand - (cum - qty), 0, qty)
            migrationBuilder.Sql(@"
WITH ranked AS (
    SELECT i.""Id"" AS item_id,
           i.""Quantity"" AS qty,
           sr.""BranchId"" AS branch_id,
           i.""ProductId"" AS product_id,
           SUM(i.""Quantity"") OVER (
               PARTITION BY sr.""BranchId"", i.""ProductId""
               ORDER BY i.""CreatedAt"" DESC, i.""Id"" DESC
               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
           ) AS cum
    FROM ""StockReceiptItems"" i
    JOIN ""StockReceipts"" sr ON sr.""Id"" = i.""StockReceiptId""
    WHERE sr.""BranchId"" IS NOT NULL
)
UPDATE ""StockReceiptItems"" t
SET ""RemainingQuantity"" = GREATEST(0, LEAST(r.qty, bs.""Quantity"" - (r.cum - r.qty)))
FROM ranked r
JOIN ""BranchStocks"" bs
    ON bs.""BranchId"" = r.branch_id AND bs.""ProductId"" = r.product_id
WHERE t.""Id"" = r.item_id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
