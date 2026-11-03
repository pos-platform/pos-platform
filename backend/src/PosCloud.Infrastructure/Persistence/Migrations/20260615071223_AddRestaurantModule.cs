using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuestCount",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargeAmount",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargePercent",
                table: "Sales",
                type: "numeric(9,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TableId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WaiterId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "SaleItems",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SentToKitchen",
                table: "SaleItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrepMinutes",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessType",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DishIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DishProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DishIngredients_Products_DishProductId",
                        column: x => x.DishProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishIngredients_Products_IngredientProductId",
                        column: x => x.IngredientProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitchenIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChefUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenIssues_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KitchenIssues_Users_ChefUserId",
                        column: x => x.ChefUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KitchenTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WaiterName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Printed = table.Column<bool>(type: "boolean", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenTickets_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hall = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantTables_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KitchenIssueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KitchenIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    IngredientProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenIssueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenIssueItems_KitchenIssues_KitchenIssueId",
                        column: x => x.KitchenIssueId,
                        principalTable: "KitchenIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitchenIssueItems_Products_DishProductId",
                        column: x => x.DishProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KitchenIssueItems_Products_IngredientProductId",
                        column: x => x.IngredientProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitchenTicketItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KitchenTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenTicketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenTicketItems_KitchenTickets_KitchenTicketId",
                        column: x => x.KitchenTicketId,
                        principalTable: "KitchenTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TableId",
                table: "Sales",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_WaiterId",
                table: "Sales",
                column: "WaiterId");

            migrationBuilder.CreateIndex(
                name: "IX_DishIngredients_DishProductId",
                table: "DishIngredients",
                column: "DishProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DishIngredients_IngredientProductId",
                table: "DishIngredients",
                column: "IngredientProductId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssueItems_DishProductId",
                table: "KitchenIssueItems",
                column: "DishProductId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssueItems_IngredientProductId",
                table: "KitchenIssueItems",
                column: "IngredientProductId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssueItems_KitchenIssueId",
                table: "KitchenIssueItems",
                column: "KitchenIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssues_BranchId",
                table: "KitchenIssues",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssues_ChefUserId",
                table: "KitchenIssues",
                column: "ChefUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssues_CreatedAt",
                table: "KitchenIssues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenIssues_Number",
                table: "KitchenIssues",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTicketItems_KitchenTicketId",
                table: "KitchenTicketItems",
                column: "KitchenTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_Printed",
                table: "KitchenTickets",
                column: "Printed");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTickets_SaleId",
                table: "KitchenTickets",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_BranchId",
                table: "RestaurantTables",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_RestaurantTables_TableId",
                table: "Sales",
                column: "TableId",
                principalTable: "RestaurantTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_WaiterId",
                table: "Sales",
                column: "WaiterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_RestaurantTables_TableId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_WaiterId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "DishIngredients");

            migrationBuilder.DropTable(
                name: "KitchenIssueItems");

            migrationBuilder.DropTable(
                name: "KitchenTicketItems");

            migrationBuilder.DropTable(
                name: "RestaurantTables");

            migrationBuilder.DropTable(
                name: "KitchenIssues");

            migrationBuilder.DropTable(
                name: "KitchenTickets");

            migrationBuilder.DropIndex(
                name: "IX_Sales_Status",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TableId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_WaiterId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "GuestCount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ServiceChargeAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ServiceChargePercent",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WaiterId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "SentToKitchen",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PrepMinutes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Organizations");
        }
    }
}
