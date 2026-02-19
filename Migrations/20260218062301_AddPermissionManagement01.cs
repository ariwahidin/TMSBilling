using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionManagement01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Icon", "IsActive", "Name", "OrderIndex", "ParentId", "PermissionCode", "Url" },
                values: new object[] { 30, "fa fa-key", true, "Permissions", 3, 7, null, "/Permission/Index" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Icon", "IsActive", "Name", "OrderIndex", "ParentId", "PermissionCode", "Url" },
                values: new object[] { 30, "fa fa-key", true, "Permissions", 3, 7, null, "/Permission/Index" });
        }
    }
}
