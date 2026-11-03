using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReceiptItemRemainingQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReceiptItems_ProductId",
                table: "StockReceiptItems");

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingQuantity",
                table: "StockReceiptItems",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptItems_ProductId_RemainingQuantity",
                table: "StockReceiptItems",
                columns: new[] { "ProductId", "RemainingQuantity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReceiptItems_ProductId_RemainingQuantity",
                table: "StockReceiptItems");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "StockReceiptItems");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptItems_ProductId",
                table: "StockReceiptItems",
                column: "ProductId");
        }
    }
}
