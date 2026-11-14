using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnsAndErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyPointsReversed",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyWithheldAmount",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RedeemedPointsReturned",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundCashAmount",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Sales",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalSaleItemId",
                table: "SaleItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedQuantity",
                table: "SaleItems",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptWhenClosed",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CourierOfferMode",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CourierOfferTimeoutSec",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.AddColumn<int>(
                name: "ReturnDays",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnPickupByCourier",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnsEnabled",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CourierAcceptedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourierOfferStatus",
                table: "OnlineOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreorder",
                table: "OnlineOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreorderNotifiedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedReturnedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "OnlineOrderItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectedQuantity",
                table: "OnlineOrderItems",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "OnlineOrderItems",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfter",
                table: "LoyaltyTransactions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "LoyaltyTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "LoyaltyTransactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourierOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierOffers_OnlineOrders_OnlineOrderId",
                        column: x => x.OnlineOrderId,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierOffers_Users_CourierUserId",
                        column: x => x.CourierUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltySettingsChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltySettingsChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoUrls = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    RefundMethod = table.Column<int>(type: "integer", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CourierPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundSaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LoyaltyWithheldAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PickupAssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineReturns_OnlineOrders_OnlineOrderId",
                        column: x => x.OnlineOrderId,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffDeviceTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Device = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemErrors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineOrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineReturnItems_OnlineReturns_OnlineReturnId",
                        column: x => x.OnlineReturnId,
                        principalTable: "OnlineReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_IsPreorder_Status_ScheduledFor",
                table: "OnlineOrders",
                columns: new[] { "IsPreorder", "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOffers_CourierUserId_Status",
                table: "CourierOffers",
                columns: new[] { "CourierUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOffers_OnlineOrderId_CourierUserId",
                table: "CourierOffers",
                columns: new[] { "OnlineOrderId", "CourierUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOffers_Status_ExpiresAt",
                table: "CourierOffers",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltySettingsChanges_OrganizationId_ChangedAt",
                table: "LoyaltySettingsChanges",
                columns: new[] { "OrganizationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturnItems_OnlineReturnId",
                table: "OnlineReturnItems",
                column: "OnlineReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturns_BranchId_Status",
                table: "OnlineReturns",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturns_CourierId_Status",
                table: "OnlineReturns",
                columns: new[] { "CourierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturns_OnlineOrderId",
                table: "OnlineReturns",
                column: "OnlineOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturns_OrganizationId_Number",
                table: "OnlineReturns",
                columns: new[] { "OrganizationId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineReturns_ShopUserId_CreatedAt",
                table: "OnlineReturns",
                columns: new[] { "ShopUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffDeviceTokens_Token",
                table: "StaffDeviceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffDeviceTokens_UserId",
                table: "StaffDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrors_Fingerprint",
                table: "SystemErrors",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrors_LastSeenAt",
                table: "SystemErrors",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrors_ResolvedAt_LastSeenAt",
                table: "SystemErrors",
                columns: new[] { "ResolvedAt", "LastSeenAt" });

            // Eski to'liq qaytarilgan savdolar: qatorlar to'liq qaytarilgan deb belgilanadi (qisman qaytarish va sotuvlar logi uchun);
            // eski refund savdolarida mijozga berilgan pul = TotalAmount.
            migrationBuilder.Sql(@"UPDATE ""SaleItems"" si SET ""RefundedQuantity"" = si.""Quantity""
                FROM ""Sales"" s WHERE s.""Id"" = si.""SaleId"" AND s.""Status"" = 3 AND s.""Type"" = 0;");
            migrationBuilder.Sql(@"UPDATE ""Sales"" SET ""RefundCashAmount"" = ""TotalAmount"" WHERE ""Type"" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierOffers");

            migrationBuilder.DropTable(
                name: "LoyaltySettingsChanges");

            migrationBuilder.DropTable(
                name: "OnlineReturnItems");

            migrationBuilder.DropTable(
                name: "StaffDeviceTokens");

            migrationBuilder.DropTable(
                name: "SystemErrors");

            migrationBuilder.DropTable(
                name: "OnlineReturns");

            migrationBuilder.DropIndex(
                name: "IX_OnlineOrders_IsPreorder_Status_ScheduledFor",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsReversed",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "LoyaltyWithheldAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RedeemedPointsReturned",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundCashAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OriginalSaleItemId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "RefundedQuantity",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "AcceptWhenClosed",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "CourierOfferMode",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "CourierOfferTimeoutSec",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ReturnDays",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ReturnPickupByCourier",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ReturnsEnabled",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "CourierAcceptedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "CourierOfferStatus",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "IsPreorder",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "PreorderNotifiedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "RejectedReturnedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "OnlineOrderItems");

            migrationBuilder.DropColumn(
                name: "RejectedQuantity",
                table: "OnlineOrderItems");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "OnlineOrderItems");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "LoyaltyTransactions");
        }
    }
}
