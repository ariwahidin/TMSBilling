using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablePriceBuy01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "TRC_ORDER",
            //    columns: table => new
            //    {
            //        id_seq = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        wh_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        sub_custid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        cnee_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        delivery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        dest_area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        tot_pkgs = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
            //        uom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        pallet_consume = table.Column<int>(type: "int", nullable: true),
            //        pallet_delivery = table.Column<int>(type: "int", nullable: true),
            //        si_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        do_rcv_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        do_rcv_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        moda_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        serv_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        truck_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        remark = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        order_status = table.Column<byte>(type: "tinyint", nullable: true),
            //        entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_TRC_ORDER", x => x.id_seq);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "TRC_ORDER_DTL",
            //    columns: table => new
            //    {
            //        id_seq = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        id_seq_order = table.Column<int>(type: "int", nullable: false),
            //        wh_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        sub_custid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        cnee_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        delivery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        gi_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        pu_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        item_category = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        pkg_unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
            //        item_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        item_length = table.Column<int>(type: "int", nullable: true),
            //        item_width = table.Column<int>(type: "int", nullable: true),
            //        item_height = table.Column<int>(type: "int", nullable: true),
            //        item_wgt = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
            //        pack_unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
            //        pallet_qty = table.Column<int>(type: "int", nullable: true),
            //        koli_qty = table.Column<int>(type: "int", nullable: true),
            //        full_addres = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
            //        entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
            //        update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            //        update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_TRC_ORDER_DTL", x => x.id_seq);
            //    });

            migrationBuilder.CreateTable(
                name: "TRC_PRICEBUY",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sup_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    buy1 = table.Column<decimal>(type: "money", nullable: true),
                    buy2 = table.Column<decimal>(type: "money", nullable: true),
                    buy3 = table.Column<decimal>(type: "money", nullable: true),
                    buy_ret_empt = table.Column<decimal>(type: "money", nullable: true),
                    buy_ret_cargo = table.Column<decimal>(type: "money", nullable: true),
                    buy_ovnight = table.Column<decimal>(type: "money", nullable: true),
                    buy_cancel = table.Column<decimal>(type: "money", nullable: true),
                    buytrip2 = table.Column<decimal>(type: "money", nullable: true),
                    buytrip3 = table.Column<decimal>(type: "money", nullable: true),
                    buy_diff_area = table.Column<decimal>(type: "money", nullable: true),
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
                    table.PrimaryKey("PK_TRC_PRICEBUY", x => x.id_seq);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "TRC_ORDER");

            //migrationBuilder.DropTable(
            //    name: "TRC_ORDER_DTL");

            migrationBuilder.DropTable(
                name: "TRC_PRICEBUY");
        }
    }
}
