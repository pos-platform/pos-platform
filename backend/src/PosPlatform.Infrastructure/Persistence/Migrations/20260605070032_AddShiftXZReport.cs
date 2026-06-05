using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftXZReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShiftReportPrinted",
                table: "CashShifts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TransferSalesNet",
                table: "CashShifts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftReportPrinted",
                table: "CashShifts");

            migrationBuilder.DropColumn(
                name: "TransferSalesNet",
                table: "CashShifts");
        }
    }
}
