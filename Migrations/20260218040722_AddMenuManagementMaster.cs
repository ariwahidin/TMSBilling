using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuManagementMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Icon", "IsActive", "Name", "OrderIndex", "ParentId", "PermissionCode", "Url" },
                values: new object[,]
                {
                    { 11, "fa-database", true, "Vendor Truck", 4, 1, null, "/Vendor/Index" },
                    { 12, "fa-database", true, "Vendor Vechile", 5, 1, null, "/VendorTruck/Index" },
                    { 13, "fa-database", true, "Driver", 6, 1, null, "/Driver/Index" },
                    { 14, "fa-database", true, "Truck Size", 7, 1, null, "/TruckSize/Index" },
                    { 15, "fa-database", true, "Origin Area", 8, 1, null, "/Origin/Index" },
                    { 16, "fa-database", true, "Destination Area", 9, 1, null, "/Destination/Index" },
                    { 17, "fa-database", true, "Warehouse", 10, 1, null, "/Warehouse/Index" },
                    { 18, "fa-database", true, "Service Moda", 11, 1, null, "/ServiceModa/Index" },
                    { 19, "fa-database", true, "Service Type", 12, 1, null, "/ServiceType/Index" },
                    { 20, "fa-database", true, "Charge UoM", 13, 1, null, "/ChargeUom/Index" },
                    { 21, "fa-database", true, "Area Group", 14, 1, null, "/AreaGroup/Index" },
                    { 22, "fa-database", true, "Price Buy", 15, 1, null, "/PriceBuy/Index" },
                    { 23, "fa-database", true, "Price Sell", 16, 1, null, "/PriceSell/Index" },
                    { 24, "fa-database", true, "Product", 17, 1, null, "/Product/Index" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 24);
        }
    }
}
