using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_DRIVER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    driver_id = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    driver_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vendor_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    driver_birth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    driver_address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    driver_sim = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_sim_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    driver_nik = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_status = table.Column<byte>(type: "tinyint", nullable: true),
                    driver_remark = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_date_terminate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    terminate_reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    user_entry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_entry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    user_update = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_update = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_photo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_DRIVER", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_DRIVER");
        }
    }
}
