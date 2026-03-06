using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_LEADTIME",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cust_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    dest = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    serv_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    serv_moda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    truck_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lead_time_days = table.Column<int>(type: "int", nullable: true),
                    lead_time_hours = table.Column<int>(type: "int", nullable: true),
                    active_flag = table.Column<byte>(type: "tinyint", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_LEADTIME", x => x.id_seq);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_LEADTIME");
        }
    }
}
