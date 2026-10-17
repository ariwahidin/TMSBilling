using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddDepatute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ACTUAL_DEPARTURE_AT",
                table: "TRC_ROUTE_PLAN_STOPS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PLANNED_DEPARTURE",
                table: "TRC_ROUTE_PLAN_STOPS",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACTUAL_DEPARTURE_AT",
                table: "TRC_ROUTE_PLAN_STOPS");

            migrationBuilder.DropColumn(
                name: "PLANNED_DEPARTURE",
                table: "TRC_ROUTE_PLAN_STOPS");
        }
    }
}
