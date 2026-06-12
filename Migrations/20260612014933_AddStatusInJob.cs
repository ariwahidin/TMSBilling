using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusInJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "job_finish_time",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "job_in_plan",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "job_is_finish",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "job_on_delivery",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "job_on_delivery_time",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "job_plan_time",
                table: "TRC_JOB_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "main_cust",
                table: "TRC_JOB_H",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "order_finish_time",
                table: "TRC_JOB",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "order_in_plan",
                table: "TRC_JOB",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "order_is_finish",
                table: "TRC_JOB",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "order_on_delivery",
                table: "TRC_JOB",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "order_on_delivery_time",
                table: "TRC_JOB",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "order_plan_time",
                table: "TRC_JOB",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_finish_time",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_in_plan",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_is_finish",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_on_delivery",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_on_delivery_time",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_plan_time",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "main_cust",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "order_finish_time",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "order_in_plan",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "order_is_finish",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "order_on_delivery",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "order_on_delivery_time",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "order_plan_time",
                table: "TRC_JOB");
        }
    }
}
