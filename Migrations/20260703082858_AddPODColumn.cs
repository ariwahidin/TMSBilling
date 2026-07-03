using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPODColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dropped_by",
                table: "TRC_JOB_POD",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dropped_on",
                table: "TRC_JOB_POD",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "picked_by",
                table: "TRC_JOB_POD",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "picked_on",
                table: "TRC_JOB_POD",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dropped_by",
                table: "TRC_JOB_POD");

            migrationBuilder.DropColumn(
                name: "dropped_on",
                table: "TRC_JOB_POD");

            migrationBuilder.DropColumn(
                name: "picked_by",
                table: "TRC_JOB_POD");

            migrationBuilder.DropColumn(
                name: "picked_on",
                table: "TRC_JOB_POD");
        }
    }
}
