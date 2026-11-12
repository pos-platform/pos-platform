using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyCashback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_CustomerId",
                table: "LoyaltyTransactions");

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyPointsRedeemed",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "LoyaltyTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "LoyaltyTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyCategoryRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyCategoryRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyCategoryRates_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CashbackPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinSaleAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxRedeemPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PointsExpireDays = table.Column<int>(type: "integer", nullable: true),
                    SignupBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EarnOnDiscountedItems = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltySettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerId_CreatedAt",
                table: "LoyaltyTransactions",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_OrganizationId_CreatedAt",
                table: "LoyaltyTransactions",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCategoryRates_CategoryId",
                table: "LoyaltyCategoryRates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCategoryRates_OrganizationId_CategoryId",
                table: "LoyaltyCategoryRates",
                columns: new[] { "OrganizationId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltySettings_OrganizationId",
                table: "LoyaltySettings",
                column: "OrganizationId",
                unique: true);

            // Eski yozuvlar: musbat → Earn (0), manfiy → Redeem (1); online buyurtma yozuvlari → Source = Online (1).
            migrationBuilder.Sql("""
                UPDATE "LoyaltyTransactions" SET "Type" = CASE WHEN "Points" < 0 THEN 1 ELSE 0 END;
                UPDATE "LoyaltyTransactions" SET "Source" = 1 WHERE "Comment" LIKE 'Online #%';
                UPDATE "LoyaltyTransactions" t SET "Source" = 1
                  FROM "Sales" s WHERE t."SaleId" = s."Id" AND s."ClientSaleId" LIKE 'online:%';
                """);

            // Online buyurtma bekor qilinganda qaytarilgan bonus — Refund (5).
            migrationBuilder.Sql("""
                UPDATE "LoyaltyTransactions" SET "Type" = 5
                 WHERE "Points" > 0 AND "Comment" LIKE 'Online #%bonus qaytarildi';
                """);

            // Oxirgi ball harakati — mavjud tranzaksiyalardan (ball muddati shu bo'yicha hisoblanadi).
            migrationBuilder.Sql("""
                UPDATE "Customers" c SET "LastActivityAt" = x.last
                  FROM (SELECT "CustomerId", MAX("CreatedAt") AS last FROM "LoyaltyTransactions" GROUP BY "CustomerId") x
                 WHERE x."CustomerId" = c."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyCategoryRates");

            migrationBuilder.DropTable(
                name: "LoyaltySettings");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_CustomerId_CreatedAt",
                table: "LoyaltyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyTransactions_OrganizationId_CreatedAt",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsRedeemed",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerId",
                table: "LoyaltyTransactions",
                column: "CustomerId");
        }
    }
}
