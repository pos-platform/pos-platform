using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditMoneyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_ReceiptNumber",
                table: "Sales");

            migrationBuilder.AddColumn<bool>(
                name: "StockShortage",
                table: "Sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "CashMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OrganizationId_ReceiptNumber",
                table: "Sales",
                columns: new[] { "OrganizationId", "ReceiptNumber" },
                unique: true);

            // Qaytarish endi "sales.refund" ruxsati bilan tekshiriladi. Ilgari har qanday kassir qaytara olardi —
            // ishlab turgan kassalar to'xtab qolmasligi uchun sotuv qila oladigan rollarga bu ruxsatni bir marta beramiz
            // (keyin administrator rollarda o'zi boshqaradi).
            migrationBuilder.Sql("""
                INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
                SELECT DISTINCT rp."RoleId", refund."Id"
                FROM "RolePermissions" rp
                JOIN "Permissions" create_perm ON create_perm."Id" = rp."PermissionId" AND create_perm."Code" = 'sales.create'
                JOIN "Permissions" refund ON refund."Code" = 'sales.refund'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "RolePermissions" x
                    WHERE x."RoleId" = rp."RoleId" AND x."PermissionId" = refund."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_OrganizationId_ReceiptNumber",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "StockShortage",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CashMovements");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ReceiptNumber",
                table: "Sales",
                column: "ReceiptNumber",
                unique: true);
        }
    }
}
