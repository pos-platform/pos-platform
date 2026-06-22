using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMarkingAndPackageCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarkingCode",
                table: "Products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageCode",
                table: "Products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarkingCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackageCode",
                table: "Products");
        }
    }
}
