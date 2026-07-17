using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleTransferSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferSource",
                table: "Sales",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferSource",
                table: "Sales");
        }
    }
}
