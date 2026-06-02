using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOrganizationIdToAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "WriteOffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "WriteOffItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SupplierPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "StockReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "StockReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SaleItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Promotions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "DebtorTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Debtors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "CashShifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Cashboxes",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "WriteOffs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "WriteOffItems");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "StockReceiptItems");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "DebtorTransactions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Debtors");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "CashShifts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Cashboxes");
        }
    }
}
