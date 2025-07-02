using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableVendorTruck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_VENDOR_TRUCK",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sup_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vehicle_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    vehicle_merk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_doortype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_driver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_STNK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_STNK_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_KIR = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_KIR_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_emisi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_active = table.Column<byte>(type: "tinyint", nullable: true),
                    vehicle_remark = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_KTP = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    vehicle_SIM = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_VENDOR_TRUCK", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_VENDOR_TRUCK");
        }
    }
}
