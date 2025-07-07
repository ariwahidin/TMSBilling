using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablePriceSell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_PRICESELL",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cust_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    origin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dest = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    serv_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    serv_moda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    truck_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    charge_uom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    flag_min = table.Column<byte>(type: "tinyint", nullable: true),
                    charge_min = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    flag_range = table.Column<byte>(type: "tinyint", nullable: true),
                    min_range = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    max_range = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    sell1 = table.Column<decimal>(type: "money", nullable: true),
                    sell2 = table.Column<decimal>(type: "money", nullable: true),
                    sell3 = table.Column<decimal>(type: "money", nullable: true),
                    sell_ret_empty = table.Column<decimal>(type: "money", nullable: true),
                    sell_ret_cargo = table.Column<decimal>(type: "money", nullable: true),
                    sell_ovnight = table.Column<decimal>(type: "money", nullable: true),
                    sell_cancel = table.Column<decimal>(type: "money", nullable: true),
                    selltrip2 = table.Column<decimal>(type: "money", nullable: true),
                    selltrip3 = table.Column<decimal>(type: "money", nullable: true),
                    sell_diff_area = table.Column<decimal>(type: "money", nullable: true),
                    valid_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    active_flag = table.Column<byte>(type: "tinyint", nullable: true),
                    curr = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    rate_value = table.Column<decimal>(type: "money", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICESELL", x => x.id_seq);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_PRICESELL");
        }
    }
}
