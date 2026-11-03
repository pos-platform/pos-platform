using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PosCloud.Infrastructure.Persistence;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// VMQ-943 chek rekvizitlari: aralash to'lovning karta qismi (Sales.CardAmount — naqd/karta
    /// fiskal chekda ajratiladi) va komitent STIR/JSHSHIR (Products/SaleItems.CommitentTin —
    /// OFD CommissionInfo). Eski yozuvlar uchun 0 / null.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261026090000_AddFiscalReceiptRequisites")]
    public partial class AddFiscalReceiptRequisites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CardAmount",
                table: "Sales",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CommitentTin",
                table: "Products",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitentTin",
                table: "SaleItems",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            // Avval yozilgan aralash to'lovlar butunlay karta sifatida fiskallangan edi —
            // hisobotlar shu bilan mos bo'lishi uchun karta qismini jamiga tenglaymiz.
            migrationBuilder.Sql(@"UPDATE ""Sales"" SET ""CardAmount"" = ""TotalAmount"" WHERE ""PaymentMethod"" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardAmount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CommitentTin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CommitentTin",
                table: "SaleItems");
        }
    }
}
