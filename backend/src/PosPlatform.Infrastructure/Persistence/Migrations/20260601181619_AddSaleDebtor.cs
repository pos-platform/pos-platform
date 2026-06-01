using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleDebtor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DebtorId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_DebtorId",
                table: "Sales",
                column: "DebtorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Debtors_DebtorId",
                table: "Sales",
                column: "DebtorId",
                principalTable: "Debtors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Debtors_DebtorId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_DebtorId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DebtorId",
                table: "Sales");
        }
    }
}
