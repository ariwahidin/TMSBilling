using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorTruckEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_VENDOR_TRUCK_EMAIL",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sup_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vehicle_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email_address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    email_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<byte>(type: "tinyint", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_VENDOR_TRUCK_EMAIL", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_VENDOR_TRUCK_EMAIL");
        }
    }
}
