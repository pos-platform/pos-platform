using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "ShopUsers",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefereeBonus",
                table: "OnlineStoreSettings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ReferralEnabled",
                table: "OnlineStoreSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferrerBonus",
                table: "OnlineStoreSettings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SupportPhone",
                table: "OnlineStoreSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportTelegram",
                table: "OnlineStoreSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatedAt",
                table: "OnlineOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "OnlineOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingComment",
                table: "OnlineOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingTags",
                table: "OnlineOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MinSpend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashbackPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    LinkType = table.Column<int>(type: "integer", nullable: false),
                    LinkValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopBanners_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopCartSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemindedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCartSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCartSnapshots_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopCartSnapshots_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnHome = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCollections_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopFavorites_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopFavorites_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopPushCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Body = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LinkType = table.Column<int>(type: "integer", nullable: false),
                    LinkValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPushCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopPushCampaigns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShopReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefereeShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferrerBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefereeBonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopReferrals_ShopUsers_RefereeShopUserId",
                        column: x => x.RefereeShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopReferrals_ShopUsers_ReferrerShopUserId",
                        column: x => x.ReferrerShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopStockSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStockSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopStockSubscriptions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopStockSubscriptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopStockSubscriptions_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopStories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopStories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopCollectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCollectionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopCollectionItems_ShopCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "ShopCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopStorySlides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkType = table.Column<int>(type: "integer", nullable: false),
                    LinkValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationSec = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStorySlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopStorySlides_ShopStories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "ShopStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopUsers_ReferralCode",
                table: "ShopUsers",
                column: "ReferralCode",
                unique: true,
                filter: "\"ReferralCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_OrganizationId_RatedAt",
                table: "OnlineOrders",
                columns: new[] { "OrganizationId", "RatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTiers_OrganizationId",
                table: "LoyaltyTiers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopBanners_BranchId",
                table: "ShopBanners",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopBanners_OrganizationId_SortOrder",
                table: "ShopBanners",
                columns: new[] { "OrganizationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopCartSnapshots_BranchId",
                table: "ShopCartSnapshots",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCartSnapshots_ChangedAt",
                table: "ShopCartSnapshots",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCartSnapshots_ShopUserId_BranchId",
                table: "ShopCartSnapshots",
                columns: new[] { "ShopUserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopCollectionItems_CollectionId_ProductId",
                table: "ShopCollectionItems",
                columns: new[] { "CollectionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopCollectionItems_ProductId",
                table: "ShopCollectionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCollections_BranchId",
                table: "ShopCollections",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCollections_OrganizationId_SortOrder",
                table: "ShopCollections",
                columns: new[] { "OrganizationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopFavorites_ProductId",
                table: "ShopFavorites",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopFavorites_ShopUserId_ProductId",
                table: "ShopFavorites",
                columns: new[] { "ShopUserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPushCampaigns_BranchId",
                table: "ShopPushCampaigns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPushCampaigns_OrganizationId_CreatedAt",
                table: "ShopPushCampaigns",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPushCampaigns_Status_ScheduledAt",
                table: "ShopPushCampaigns",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopReferrals_OrganizationId_RefereeShopUserId",
                table: "ShopReferrals",
                columns: new[] { "OrganizationId", "RefereeShopUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopReferrals_OrganizationId_ReferrerShopUserId",
                table: "ShopReferrals",
                columns: new[] { "OrganizationId", "ReferrerShopUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopReferrals_RefereeShopUserId",
                table: "ShopReferrals",
                column: "RefereeShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopReferrals_ReferrerShopUserId",
                table: "ShopReferrals",
                column: "ReferrerShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStockSubscriptions_BranchId",
                table: "ShopStockSubscriptions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStockSubscriptions_NotifiedAt",
                table: "ShopStockSubscriptions",
                column: "NotifiedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStockSubscriptions_ProductId",
                table: "ShopStockSubscriptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStockSubscriptions_ShopUserId_ProductId_BranchId",
                table: "ShopStockSubscriptions",
                columns: new[] { "ShopUserId", "ProductId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopStories_BranchId",
                table: "ShopStories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStories_OrganizationId_SortOrder",
                table: "ShopStories",
                columns: new[] { "OrganizationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopStorySlides_StoryId",
                table: "ShopStorySlides",
                column: "StoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyTiers");

            migrationBuilder.DropTable(
                name: "ShopBanners");

            migrationBuilder.DropTable(
                name: "ShopCartSnapshots");

            migrationBuilder.DropTable(
                name: "ShopCollectionItems");

            migrationBuilder.DropTable(
                name: "ShopFavorites");

            migrationBuilder.DropTable(
                name: "ShopPushCampaigns");

            migrationBuilder.DropTable(
                name: "ShopReferrals");

            migrationBuilder.DropTable(
                name: "ShopStockSubscriptions");

            migrationBuilder.DropTable(
                name: "ShopStorySlides");

            migrationBuilder.DropTable(
                name: "ShopCollections");

            migrationBuilder.DropTable(
                name: "ShopStories");

            migrationBuilder.DropIndex(
                name: "IX_ShopUsers_ReferralCode",
                table: "ShopUsers");

            migrationBuilder.DropIndex(
                name: "IX_OnlineOrders_OrganizationId_RatedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "ShopUsers");

            migrationBuilder.DropColumn(
                name: "RefereeBonus",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ReferralEnabled",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "ReferrerBonus",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "SupportPhone",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "SupportTelegram",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "RatedAt",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "RatingComment",
                table: "OnlineOrders");

            migrationBuilder.DropColumn(
                name: "RatingTags",
                table: "OnlineOrders");
        }
    }
}
