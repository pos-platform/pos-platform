using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndShiftTerminalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TerminalId",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminalId",
                table: "CashShifts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "CashShifts");
        }
    }
}
