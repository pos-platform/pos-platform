using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyPercentPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CashbackPercent",
                table: "LoyaltyTiers",
                type: "numeric(8,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CashbackPercent",
                table: "LoyaltySettings",
                type: "numeric(8,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Percent",
                table: "LoyaltyCategoryRates",
                type: "numeric(8,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CashbackPercent",
                table: "LoyaltyTiers",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CashbackPercent",
                table: "LoyaltySettings",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Percent",
                table: "LoyaltyCategoryRates",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)");
        }
    }
}
