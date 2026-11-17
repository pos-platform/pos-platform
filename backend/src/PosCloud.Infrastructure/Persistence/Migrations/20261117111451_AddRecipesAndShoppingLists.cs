using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipesAndShoppingLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopLists_ShopUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CookMinutes = table.Column<int>(type: "integer", nullable: true),
                    Servings = table.Column<int>(type: "integer", nullable: true),
                    Steps = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnHome = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopRecipes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedByName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopListItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShopListItems_ShopLists_ListId",
                        column: x => x.ListId,
                        principalTable: "ShopLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopListMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopListMembers_ShopLists_ListId",
                        column: x => x.ListId,
                        principalTable: "ShopLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopListMembers_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShopRecipeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Note = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRecipeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopRecipeItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopRecipeItems_ShopRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "ShopRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopListItems_ListId",
                table: "ShopListItems",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListItems_ProductId",
                table: "ShopListItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListMembers_ListId_Phone",
                table: "ShopListMembers",
                columns: new[] { "ListId", "Phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopListMembers_Phone",
                table: "ShopListMembers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListMembers_ShopUserId",
                table: "ShopListMembers",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopLists_OwnerId",
                table: "ShopLists",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRecipeItems_ProductId",
                table: "ShopRecipeItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRecipeItems_RecipeId_ProductId",
                table: "ShopRecipeItems",
                columns: new[] { "RecipeId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopRecipes_BranchId",
                table: "ShopRecipes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRecipes_OrganizationId_SortOrder",
                table: "ShopRecipes",
                columns: new[] { "OrganizationId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopListItems");

            migrationBuilder.DropTable(
                name: "ShopListMembers");

            migrationBuilder.DropTable(
                name: "ShopRecipeItems");

            migrationBuilder.DropTable(
                name: "ShopLists");

            migrationBuilder.DropTable(
                name: "ShopRecipes");
        }
    }
}
