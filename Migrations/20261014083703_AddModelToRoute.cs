using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddModelToRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_ROUTES",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROUTE_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ROUTE_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ROUTE_TYPE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CREATED_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_CUSTOMERS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROUTE_ID = table.Column<int>(type: "int", nullable: false),
                    CUSTOMER_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_CUSTOMERS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TRC_ROUTE_CUSTOMERS_TRC_ROUTES_ROUTE_ID",
                        column: x => x.ROUTE_ID,
                        principalTable: "TRC_ROUTES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_STOPS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROUTE_ID = table.Column<int>(type: "int", nullable: false),
                    GEOFENCE_TABLE_ID = table.Column<int>(type: "int", nullable: false),
                    SEQUENCE_NO = table.Column<int>(type: "int", nullable: false),
                    STOP_TYPE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PLANNED_ARRIVAL = table.Column<TimeSpan>(type: "time", nullable: true),
                    STOP_DURATION_MINUTES = table.Column<int>(type: "int", nullable: true),
                    NOTES = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_STOPS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TRC_ROUTE_STOPS_TRC_GEOFENCE_GEOFENCE_TABLE_ID",
                        column: x => x.GEOFENCE_TABLE_ID,
                        principalTable: "TRC_GEOFENCE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TRC_ROUTE_STOPS_TRC_ROUTES_ROUTE_ID",
                        column: x => x.ROUTE_ID,
                        principalTable: "TRC_ROUTES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_ROUTE_CUSTOMERS_ROUTE_ID",
                table: "TRC_ROUTE_CUSTOMERS",
                column: "ROUTE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_ROUTE_STOPS_GEOFENCE_TABLE_ID",
                table: "TRC_ROUTE_STOPS",
                column: "GEOFENCE_TABLE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_ROUTE_STOPS_ROUTE_ID",
                table: "TRC_ROUTE_STOPS",
                column: "ROUTE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_ROUTE_CUSTOMERS");

            migrationBuilder.DropTable(
                name: "TRC_ROUTE_STOPS");

            migrationBuilder.DropTable(
                name: "TRC_ROUTES");
        }
    }
}
