using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreWhiteLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AndroidLatestBuild",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AndroidLatestVersion",
                table: "OnlineStoreSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AndroidMinBuild",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AndroidStoreUrl",
                table: "OnlineStoreSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppName",
                table: "OnlineStoreSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppPackageId",
                table: "OnlineStoreSettings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandColor",
                table: "OnlineStoreSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IosLatestBuild",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IosLatestVersion",
                table: "OnlineStoreSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IosMinBuild",
                table: "OnlineStoreSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IosStoreUrl",
                table: "OnlineStoreSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "OnlineStoreSettings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineStoreSettings_AppPackageId",
                table: "OnlineStoreSettings",
                column: "AppPackageId",
                unique: true,
                filter: "\"AppPackageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineStoreSettings_Slug",
                table: "OnlineStoreSettings",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OnlineStoreSettings_AppPackageId",
                table: "OnlineStoreSettings");

            migrationBuilder.DropIndex(
                name: "IX_OnlineStoreSettings_Slug",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AndroidLatestBuild",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AndroidLatestVersion",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AndroidMinBuild",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AndroidStoreUrl",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AppName",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "AppPackageId",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "BrandColor",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "IosLatestBuild",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "IosLatestVersion",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "IosMinBuild",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "IosStoreUrl",
                table: "OnlineStoreSettings");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "OnlineStoreSettings");
        }
    }
}
