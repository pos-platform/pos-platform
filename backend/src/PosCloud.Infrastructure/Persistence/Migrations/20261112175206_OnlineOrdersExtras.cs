using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnlineOrdersExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoCancelMinutes",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: false,
                // Mavjud do'konlar uchun ham 30 daqiqa (modelda HasDefaultValue yo'q — 0 "o'chiq" qiymati to'g'ri saqlanishi uchun).
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMaxMinutes",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMinMinutes",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "OnlineOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierName",
                table: "OnlineOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierPhone",
                table: "OnlineOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickingStartedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_ShopUserId_ClientRequestId",
                table: "OnlineOrders",
                columns: new[] { "ShopUserId", "ClientRequestId" },
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OnlineOrders_ShopUserId_ClientRequestId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "AutoCancelMinutes",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryMaxMinutes",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryMinMinutes",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierName",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierPhone",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "PickingStartedAt",
                table: "OnlineOrders");
        }
    }
}
