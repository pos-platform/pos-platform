using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PosCloud.Infrastructure.Persistence;

#nullable disable

namespace PosCloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260723090000_AddFiscalModules")]
    public partial class AddFiscalModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Inn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TerminalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalModules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalModules_OrganizationId_ModuleId",
                table: "FiscalModules",
                columns: new[] { "OrganizationId", "ModuleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalModules");
        }
    }
}
