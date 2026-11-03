using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashShiftsAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashShiftId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalSaleId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Sales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CashShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalSales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRefunds = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashSalesNet = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CardSalesNet = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashDifference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashShifts_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashShiftId",
                table: "Sales",
                column: "CashShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OriginalSaleId",
                table: "Sales",
                column: "OriginalSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Type",
                table: "Sales",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_BranchId",
                table: "CashShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashShifts_CashierId_Status",
                table: "CashShifts",
                columns: new[] { "CashierId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_CashShifts_CashShiftId",
                table: "Sales",
                column: "CashShiftId",
                principalTable: "CashShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Sales_OriginalSaleId",
                table: "Sales",
                column: "OriginalSaleId",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_CashShifts_CashShiftId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Sales_OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "CashShifts");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CashShiftId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_Type",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CashShiftId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Sales");
        }
    }
}
