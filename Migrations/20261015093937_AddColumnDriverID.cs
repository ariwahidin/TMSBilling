using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDriverID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DRIVER_ID",
                table: "TRC_ROUTE_PLAN_H",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VEHICLE_ID",
                table: "TRC_ROUTE_PLAN_H",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DRIVER_ID",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "VEHICLE_ID",
                table: "TRC_ROUTE_PLAN_H");
        }
    }
}
