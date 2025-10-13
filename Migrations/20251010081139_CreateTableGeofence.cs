using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableGeofence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_GEOFENCE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GEOFENCE_ID = table.Column<int>(type: "int", nullable: true),
                    COMPANY_ID = table.Column<int>(type: "int", nullable: true),
                    CUSTOMER_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FENCE_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TYPE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    POLY_DATA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CIRC_DATA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADDRESS = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ADDRESS_DETAIL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PROVINCE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CITY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    POSTAL_CODE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CATEGORY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CONTACT_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PHONE_NO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IS_GARAGE = table.Column<bool>(type: "bit", nullable: false),
                    IS_SERVICE_LOC = table.Column<bool>(type: "bit", nullable: false),
                    IS_BILLING_ADDR = table.Column<bool>(type: "bit", nullable: false),
                    IS_DEPOT = table.Column<bool>(type: "bit", nullable: false),
                    IS_ALERT = table.Column<bool>(type: "bit", nullable: false),
                    SERVICE_START = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SERVICE_END = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BREAK_START = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BREAK_END = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SERVICE_LOC_TYPE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUSTOMER_NAME = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HAS_RELATION = table.Column<bool>(type: "bit", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_GEOFENCE", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_GEOFENCE");
        }
    }
}
