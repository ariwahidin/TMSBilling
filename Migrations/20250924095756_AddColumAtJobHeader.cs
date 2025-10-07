using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumAtJobHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "charge_uom",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deliv_date",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dest",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_name",
                table: "TRC_JOB_H",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_phone",
                table: "TRC_JOB_H",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_vendor",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "multidrop",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "serv_moda",
                table: "TRC_JOB_H",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "serv_type",
                table: "TRC_JOB_H",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "truck_no",
                table: "TRC_JOB_H",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "truck_size",
                table: "TRC_JOB_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vendor_act",
                table: "TRC_JOB_H",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vendor_plan",
                table: "TRC_JOB_H",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "charge_uom",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "deliv_date",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "dest",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "driver_name",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "driver_phone",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "is_vendor",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "multidrop",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "serv_moda",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "serv_type",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "truck_no",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "truck_size",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "vendor_act",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "vendor_plan",
                table: "TRC_JOB_H");
        }
    }
}
