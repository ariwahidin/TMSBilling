using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddModelToRoutePlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_PLAN_H",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PLAN_NO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PLAN_DATE = table.Column<DateTime>(type: "date", nullable: false),
                    ROUTE_ID = table.Column<int>(type: "int", nullable: false),
                    ROUTE_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ROUTE_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CUSTOMER_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PLANNED_DEPARTURE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VEHICLE_NO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DRIVER_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NOTES = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CREATED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UPDATED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CONFIRMED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CONFIRMED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STARTED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    STARTED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    COMPLETED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    COMPLETED_AT = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_PLAN_H", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_PLAN_STOPS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROUTE_PLAN_ID = table.Column<int>(type: "int", nullable: false),
                    MASTER_ROUTE_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    GEOFENCE_TABLE_ID = table.Column<int>(type: "int", nullable: false),
                    SEQUENCE_NO = table.Column<int>(type: "int", nullable: false),
                    STOP_TYPE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FENCE_NAME = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ADDRESS = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CITY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PLANNED_ARRIVAL = table.Column<TimeSpan>(type: "time", nullable: true),
                    STOP_DURATION_MINUTES = table.Column<int>(type: "int", nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ACTUAL_ARRIVAL_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    COMPLETED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NOTES = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_PLAN_STOPS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TRC_ROUTE_PLAN_STOPS_TRC_ROUTE_PLAN_H_ROUTE_PLAN_ID",
                        column: x => x.ROUTE_PLAN_ID,
                        principalTable: "TRC_ROUTE_PLAN_H",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_PLAN_STOP_HISTORY",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROUTE_PLAN_ID = table.Column<int>(type: "int", nullable: false),
                    ROUTE_PLAN_STOP_ID = table.Column<int>(type: "int", nullable: false),
                    PREVIOUS_STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NEW_STATUS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ACTION_TYPE = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    REASON = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CHANGED_BY = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CHANGED_AT = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_PLAN_STOP_HISTORY", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TRC_ROUTE_PLAN_STOP_HISTORY_TRC_ROUTE_PLAN_STOPS_ROUTE_PLAN_STOP_ID",
                        column: x => x.ROUTE_PLAN_STOP_ID,
                        principalTable: "TRC_ROUTE_PLAN_STOPS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_ROUTE_PLAN_STOP_HISTORY_ROUTE_PLAN_STOP_ID",
                table: "TRC_ROUTE_PLAN_STOP_HISTORY",
                column: "ROUTE_PLAN_STOP_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_ROUTE_PLAN_STOPS_ROUTE_PLAN_ID",
                table: "TRC_ROUTE_PLAN_STOPS",
                column: "ROUTE_PLAN_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_ROUTE_PLAN_STOP_HISTORY");

            migrationBuilder.DropTable(
                name: "TRC_ROUTE_PLAN_STOPS");

            migrationBuilder.DropTable(
                name: "TRC_ROUTE_PLAN_H");
        }
    }
}
