using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedOpeningBalanceCostLayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bir martalik: mavjud qoldiq (import/inventarizatsiya orqali kirgan, накладнаsiz)
            // uchun boshlang'ich FIFO tannarx qatlamini yaratamiz — har mahsulot qoldig'i
            // joriy Product.CostPrice bilan. Shunda kelajakda yangi narxli kirim kelganda
            // eski qoldiq o'z eski narxida (arzon) sotiladi va foydasi yuqori chiqadi.
            //
            // Har (filial, org) uchun bitta "Boshlang'ich qoldiq" накладна + supplier yaratiladi.
            // Faqat StockReceiptItems hali bo'sh bo'lsa ishlaydi (idempotent — qayta ishlamaydi).
            // Toza/yangi bazada BranchStocks bo'sh bo'lgani uchun hech narsa yaratmaydi (no-op).
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM ""StockReceiptItems"") THEN
        RETURN; -- allaqachon qatlamlar bor, seed kerak emas
    END IF;

    CREATE TEMP TABLE _opening_keys ON COMMIT DROP AS
    SELECT d.""BranchId""        AS branch_id,
           d.""OrganizationId""  AS org_id,
           gen_random_uuid()    AS supplier_id,
           gen_random_uuid()    AS receipt_id
    FROM (SELECT DISTINCT ""BranchId"", ""OrganizationId""
          FROM ""BranchStocks"" WHERE ""Quantity"" > 0) d;

    IF NOT EXISTS (SELECT 1 FROM _opening_keys) THEN
        RETURN; -- qoldiq yo'q
    END IF;

    INSERT INTO ""Suppliers"" (""Id"",""Name"",""Balance"",""IsActive"",""CreatedAt"",""OrganizationId"")
    SELECT supplier_id, 'Boshlang''ich qoldiq', 0, true, now(), org_id
    FROM _opening_keys;

    INSERT INTO ""StockReceipts""
        (""Id"",""Number"",""SupplierId"",""BranchId"",""Comment"",""TotalAmount"",""CreatedAt"",""OrganizationId"")
    SELECT k.receipt_id,
           'OPENING-' || substr(replace(k.branch_id::text,'-',''),1,8),
           k.supplier_id, k.branch_id,
           'Boshlang''ich qoldiq (avtomatik FIFO seed)',
           COALESCE(SUM(bs.""Quantity"" * p.""CostPrice""), 0),
           now() - interval '1 second',  -- kelajakdagi kirimlardan eskiroq → FIFO birinchi shu
           k.org_id
    FROM _opening_keys k
    JOIN ""BranchStocks"" bs ON bs.""BranchId"" = k.branch_id AND bs.""Quantity"" > 0
    JOIN ""Products"" p ON p.""Id"" = bs.""ProductId""
    GROUP BY k.receipt_id, k.supplier_id, k.branch_id, k.org_id;

    INSERT INTO ""StockReceiptItems""
        (""Id"",""StockReceiptId"",""ProductId"",""Quantity"",""CostPrice"",""LineTotal"",""RemainingQuantity"",""CreatedAt"",""OrganizationId"")
    SELECT gen_random_uuid(), k.receipt_id, bs.""ProductId"",
           bs.""Quantity"", p.""CostPrice"", bs.""Quantity"" * p.""CostPrice"",
           bs.""Quantity"",                 -- RemainingQuantity = to'liq qoldiq
           now() - interval '1 second',
           k.org_id
    FROM _opening_keys k
    JOIN ""BranchStocks"" bs ON bs.""BranchId"" = k.branch_id AND bs.""Quantity"" > 0
    JOIN ""Products"" p ON p.""Id"" = bs.""ProductId"";
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
