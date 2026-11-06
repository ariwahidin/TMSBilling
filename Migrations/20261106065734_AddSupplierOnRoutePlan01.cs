using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierOnRoutePlan01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "TRC_ROUTE_PLAN_H",
                newName: "SUPPLIER_CODE");

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_1",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_2",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_3",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_CANCEL",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_CHARGE_MIN",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PRICE_BUY_CURRENCY",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_DIFF_AREA",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PRICE_BUY_FLAG_MIN",
                table: "TRC_ROUTE_PLAN_H",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PRICE_BUY_FLAG_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PRICE_BUY_ID",
                table: "TRC_ROUTE_PLAN_H",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_MAX_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_MIN_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_OVNIGHT",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_RATE_BBM",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_RATE_TOLL",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_RATE_VALUE",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_RET_CARGO",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_RET_EMPT",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_TRIP2",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_BUY_TRIP3",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PRICE_BUY_VALID_DATE",
                table: "TRC_ROUTE_PLAN_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_1",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_2",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_3",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_CANCEL",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_CHARGE_MIN",
                table: "TRC_ROUTE_PLAN_H",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PRICE_SELL_CURRENCY",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_DIFF_AREA",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PRICE_SELL_FLAG_MIN",
                table: "TRC_ROUTE_PLAN_H",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PRICE_SELL_FLAG_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PRICE_SELL_ID",
                table: "TRC_ROUTE_PLAN_H",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_MAX_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_MIN_RANGE",
                table: "TRC_ROUTE_PLAN_H",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_OVNIGHT",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RATE_DRIVER",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RATE_TOLL",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RATE_TRUCK",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RATE_VALUE",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RET_CARGO",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_RET_EMPTY",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_TRIP2",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PRICE_SELL_TRIP3",
                table: "TRC_ROUTE_PLAN_H",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PRICE_SELL_VALID_DATE",
                table: "TRC_ROUTE_PLAN_H",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TARIFF_CHARGE_UOM",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TARIFF_SERV_MODA",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TARIFF_SERV_TYPE",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TARIFF_TRUCK_SIZE",
                table: "TRC_ROUTE_PLAN_H",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PRICE_BUY_1",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_2",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_3",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_CANCEL",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_CHARGE_MIN",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_CURRENCY",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_DIFF_AREA",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_FLAG_MIN",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_FLAG_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_ID",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_MAX_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_MIN_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_OVNIGHT",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_RATE_BBM",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_RATE_TOLL",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_RATE_VALUE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_RET_CARGO",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_RET_EMPT",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_TRIP2",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_TRIP3",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_BUY_VALID_DATE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_1",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_2",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_3",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_CANCEL",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_CHARGE_MIN",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_CURRENCY",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_DIFF_AREA",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_FLAG_MIN",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_FLAG_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_ID",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_MAX_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_MIN_RANGE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_OVNIGHT",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RATE_DRIVER",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RATE_TOLL",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RATE_TRUCK",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RATE_VALUE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RET_CARGO",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_RET_EMPTY",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_TRIP2",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_TRIP3",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_VALID_DATE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "TARIFF_CHARGE_UOM",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "TARIFF_SERV_MODA",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "TARIFF_SERV_TYPE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.DropColumn(
                name: "TARIFF_TRUCK_SIZE",
                table: "TRC_ROUTE_PLAN_H");

            migrationBuilder.RenameColumn(
                name: "SUPPLIER_CODE",
                table: "TRC_ROUTE_PLAN_H",
                newName: "SupplierCode");
        }
    }
}
