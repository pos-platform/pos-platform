using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnlineShopPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowClick",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPayme",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BonusMaxPercent",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "BonusPayEnabled",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryRadiusKm",
                table: "OnlineStoreSettings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryServiceMxikCode",
                table: "OnlineStoreSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BonusCharged",
                table: "OnlineOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusUsed",
                table: "OnlineOrders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CourierCashCollected",
                table: "OnlineOrders",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CourierCashExpected",
                table: "OnlineOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CourierCashHandedOverAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CourierFailedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourierFailedByUserId",
                table: "OnlineOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourierUserId",
                table: "OnlineOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnlinePaymentProvider",
                table: "OnlineOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "OnlineOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromoCodeId",
                table: "OnlineOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromoDiscount",
                table: "OnlineOrders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<List<string>>(
                name: "MarkingCodes",
                table: "OnlineOrderItems",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    PerUserLimit = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_CourierUserId_Status",
                table: "OnlineOrders",
                columns: new[] { "CourierUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_PromoCodeId",
                table: "OnlineOrders",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_OrganizationId_Code",
                table: "PromoCodes",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineOrders_PromoCodes_PromoCodeId",
                table: "OnlineOrders",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineOrders_Users_CourierUserId",
                table: "OnlineOrders",
                column: "CourierUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnlineOrders_PromoCodes_PromoCodeId",
                table: "OnlineOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_OnlineOrders_Users_CourierUserId",
                table: "OnlineOrders");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_OnlineOrders_CourierUserId_Status",
                table: "OnlineOrders");

            migrationBuilder.DropIndex(
                name: "IX_OnlineOrders_PromoCodeId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "AllowClick",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AllowPayme",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "BonusMaxPercent",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "BonusPayEnabled",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryRadiusKm",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryServiceMxikCode",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "BonusCharged",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "BonusUsed",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierCashCollected",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierCashExpected",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierCashHandedOverAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierFailedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierFailedByUserId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierUserId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "OnlinePaymentProvider",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "PromoDiscount",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "MarkingCodes",
                table: "OnlineOrderItems");
        }
    }
}
