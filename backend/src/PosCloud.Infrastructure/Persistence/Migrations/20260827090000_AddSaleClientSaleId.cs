using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PosCloud.Infrastructure.Persistence;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Savdoga idempotentlik kaliti (ClientSaleId) qo'shadi: kassa javobni ololmay so'rovni
    /// qayta yuborganda server yangi savdo yaratmasligi uchun. Kalit yuborilmagan savdolarga
    /// cheklov qo'llanmaydi (filtrli unique indeks).
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260827090000_AddSaleClientSaleId")]
    public partial class AddSaleClientSaleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSaleId",
                table: "Sales",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ClientSaleId",
                table: "Sales",
                column: "ClientSaleId",
                unique: true,
                filter: "\"ClientSaleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_ClientSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ClientSaleId",
                table: "Sales");
        }
    }
}
