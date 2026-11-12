using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnline",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true); // mavjud mahsulotlar ilovada ko'rinadi

            migrationBuilder.AddColumn<Guid>(
                name: "ShopUserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Reserved",
                table: "BranchStocks",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Branches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Branches",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OnlineStoreSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequested = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PaidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AllowCash = table.Column<bool>(type: "boolean", nullable: false),
                    AllowTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCardOnDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    IsAcceptingOrders = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeliveryEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PickupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FreeDeliveryFrom = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    OpensAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ClosesAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    TransferCardNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TransferCardHolder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequireTransferBeforeDispatch = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineStoreSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PhoneVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FulfillmentType = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Entrance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Floor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Apartment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Intercom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Landmark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Contactless = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OutOfStockPolicy = table.Column<int>(type: "integer", nullable: false),
                    ItemsTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransferProofUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopRefreshTokens_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopUserAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Entrance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Floor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Apartment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Intercom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Landmark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUserAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopUserAddresses_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    ReplacesItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineOrderItems_OnlineOrders_OnlineOrderId",
                        column: x => x.OnlineOrderId,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnlineOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ShopUserId",
                table: "Customers",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrderItems_OnlineOrderId",
                table: "OnlineOrderItems",
                column: "OnlineOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrderItems_ProductId",
                table: "OnlineOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_BranchId_Status",
                table: "OnlineOrders",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_CustomerId",
                table: "OnlineOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_OrganizationId_Number",
                table: "OnlineOrders",
                columns: new[] { "OrganizationId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_SaleId",
                table: "OnlineOrders",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_ShopUserId_CreatedAt",
                table: "OnlineOrders",
                columns: new[] { "ShopUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineStoreSettings_OrganizationId",
                table: "OnlineStoreSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopRefreshTokens_ExpiresAt",
                table: "ShopRefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRefreshTokens_ShopUserId",
                table: "ShopRefreshTokens",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRefreshTokens_TokenHash",
                table: "ShopRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserAddresses_ShopUserId",
                table: "ShopUserAddresses",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUsers_Phone",
                table: "ShopUsers",
                column: "Phone",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_ShopUsers_ShopUserId",
                table: "Customers",
                column: "ShopUserId",
                principalTable: "ShopUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_ShopUsers_ShopUserId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "OnlineOrderItems");

            migrationBuilder.DropTable(
                name: "OnlineStoreSettings");

            migrationBuilder.DropTable(
                name: "ShopRefreshTokens");

            migrationBuilder.DropTable(
                name: "ShopUserAddresses");

            migrationBuilder.DropTable(
                name: "OnlineOrders");

            migrationBuilder.DropTable(
                name: "ShopUsers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ShopUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShowOnline",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShopUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Reserved",
                table: "BranchStocks");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Branches");
        }
    }
}
