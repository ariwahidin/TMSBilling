using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnCreatedByAtJobTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "entry_date",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entry_user",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "update_date",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "update_user",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "entry_date",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "entry_user",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "update_date",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "update_user",
                table: "TRC_JOB_H");
        }
    }
}
