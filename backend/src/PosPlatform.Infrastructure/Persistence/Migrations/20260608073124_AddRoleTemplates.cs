using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleTemplates : Migration
    {
        private static readonly Guid AdminId   = new("11111111-0000-0000-0000-000000000001");
        private static readonly Guid ManagerId = new("11111111-0000-0000-0000-000000000002");
        private static readonly Guid CashierId = new("11111111-0000-0000-0000-000000000003");

        private const string AdminPerms   = "[\"users.view\",\"users.create\",\"users.edit\",\"users.activate\",\"roles.view\",\"roles.manage\",\"branches.view\",\"branches.create\",\"branches.edit\",\"branches.delete\",\"products.view\",\"products.create\",\"products.edit\",\"products.delete\",\"categories.view\",\"categories.manage\",\"sales.view\",\"sales.create\",\"sales.refund\",\"reports.view\",\"cashbox.view\",\"cashbox.manage\",\"inventory.view\",\"inventory.manage\",\"suppliers.view\",\"suppliers.manage\",\"customers.view\",\"customers.manage\",\"debtors.view\",\"debtors.manage\",\"expenses.view\",\"expenses.manage\",\"settings.manage\"]";
        private const string ManagerPerms = "[\"users.view\",\"roles.view\",\"branches.view\",\"products.view\",\"products.create\",\"products.edit\",\"categories.view\",\"categories.manage\",\"sales.view\",\"sales.create\",\"sales.refund\",\"reports.view\",\"cashbox.view\",\"inventory.view\",\"inventory.manage\",\"suppliers.view\",\"suppliers.manage\",\"customers.view\",\"customers.manage\",\"debtors.view\",\"debtors.manage\",\"expenses.view\",\"expenses.manage\"]";
        private const string CashierPerms = "[\"products.view\",\"categories.view\",\"sales.view\",\"sales.create\",\"cashbox.view\",\"customers.view\",\"customers.manage\",\"debtors.view\"]";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleTemplates",
                columns: table => new
                {
                    Id             = table.Column<Guid>(type: "uuid", nullable: false),
                    Name           = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsSystem       = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PermissionsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    UpdatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_Name",
                table: "RoleTemplates",
                column: "Name",
                unique: true);

            // Standart rollarni seed qilamiz
            migrationBuilder.InsertData(
                table: "RoleTemplates",
                columns: ["Id", "Name", "IsSystem", "PermissionsJson", "UpdatedAt"],
                values: new object[]
                {
                    AdminId,   "Admin",   true, AdminPerms,   DateTime.UtcNow
                });
            migrationBuilder.InsertData(
                table: "RoleTemplates",
                columns: ["Id", "Name", "IsSystem", "PermissionsJson", "UpdatedAt"],
                values: new object[]
                {
                    ManagerId, "Manager", true, ManagerPerms, DateTime.UtcNow
                });
            migrationBuilder.InsertData(
                table: "RoleTemplates",
                columns: ["Id", "Name", "IsSystem", "PermissionsJson", "UpdatedAt"],
                values: new object[]
                {
                    CashierId, "Cashier", true, CashierPerms, DateTime.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RoleTemplates");
        }
    }
}
