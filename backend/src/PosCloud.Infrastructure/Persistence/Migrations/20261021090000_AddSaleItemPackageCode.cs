using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PosCloud.Infrastructure.Persistence;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Savdo qatoriga qadoq kodi (PackageCode) snapshotini qo'shadi — fiskal chek qatorida
    /// MXIK bilan birga uzatiladi. Eski qatorlar uchun null (fiskalga yuborilmaydi).
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261021090000_AddSaleItemPackageCode")]
    public partial class AddSaleItemPackageCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackageCode",
                table: "SaleItems",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageCode",
                table: "SaleItems");
        }
    }
}
