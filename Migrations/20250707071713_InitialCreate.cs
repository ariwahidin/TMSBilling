using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_AREA_GROUP",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    area_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_AREA_GROUP", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CHARGE_UOM",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    charge_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CHARGE_UOM", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CONSIGNEE",
                columns: table => new
                {
                    CNEE_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CNEE_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR1 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CNEE_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR4 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CNEE_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CNEE_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ACTIVE_FLAG = table.Column<byte>(type: "tinyint", nullable: true),
                    SUB_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CONSIGNEE", x => x.CNEE_CODE);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CUST_GROUP",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SUB_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CUST_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUST_GROUP", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CUSTOMER_MAIN",
                columns: table => new
                {
                    MAIN_CUST = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CUST_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_ADDR1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_CITY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_EMAIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CUST_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CUST_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TAX_IMG = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STATUS_FLAG = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUSTOMER_MAIN", x => x.MAIN_CUST);
                });

            migrationBuilder.CreateTable(
                name: "TRC_DESTINATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    destination_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dest_loccode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_DESTINATION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_DRIVER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    driver_id = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    driver_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vendor_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    driver_birth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    driver_address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    driver_sim = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_sim_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    driver_nik = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_status = table.Column<byte>(type: "tinyint", nullable: true),
                    driver_remark = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_date_terminate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    terminate_reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    user_entry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_entry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    user_update = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    date_update = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    driver_photo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_DRIVER", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_JOB",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobid = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    vendorid = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    truckid = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    drivername = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    moda_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    serv_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    truck_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    multidrop = table.Column<byte>(type: "tinyint", nullable: true),
                    multitrip = table.Column<byte>(type: "tinyint", nullable: true),
                    drop_seq = table.Column<int>(type: "int", nullable: true),
                    ritase_seq = table.Column<int>(type: "int", nullable: true),
                    inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    origin_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dest_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dvdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cust_ori = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    servtype_ori = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    trucksize_ori = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    origin_ori = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dest_ori = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    container_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vendor_job = table.Column<byte>(type: "tinyint", nullable: true),
                    vendorid_act = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    job_status = table.Column<int>(type: "int", nullable: true),
                    flag_pu = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_diffa = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_ep = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_rc = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_ov = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_cc = table.Column<byte>(type: "tinyint", nullable: true),
                    flag_charge = table.Column<byte>(type: "tinyint", nullable: true),
                    charge_uom_v = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    charge_uom_c = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    buy1 = table.Column<decimal>(type: "money", nullable: true),
                    buy2 = table.Column<decimal>(type: "money", nullable: true),
                    buy3 = table.Column<decimal>(type: "money", nullable: true),
                    buy_trip2 = table.Column<decimal>(type: "money", nullable: true),
                    buy_trip3 = table.Column<decimal>(type: "money", nullable: true),
                    buy_diffa = table.Column<decimal>(type: "money", nullable: true),
                    buy_ep = table.Column<decimal>(type: "money", nullable: true),
                    buy_rc = table.Column<decimal>(type: "money", nullable: true),
                    buy_ov = table.Column<decimal>(type: "money", nullable: true),
                    buy_cc = table.Column<decimal>(type: "money", nullable: true),
                    sell1 = table.Column<decimal>(type: "money", nullable: true),
                    sell2 = table.Column<decimal>(type: "money", nullable: true),
                    sell3 = table.Column<decimal>(type: "money", nullable: true),
                    sell_trip2 = table.Column<decimal>(type: "money", nullable: true),
                    sell_trip3 = table.Column<decimal>(type: "money", nullable: true),
                    sell_diffa = table.Column<decimal>(type: "money", nullable: true),
                    sell_ep = table.Column<decimal>(type: "money", nullable: true),
                    sell_rc = table.Column<decimal>(type: "money", nullable: true),
                    sell_ov = table.Column<decimal>(type: "money", nullable: true),
                    sell_cc = table.Column<decimal>(type: "money", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_JOB", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_JOB_POD",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jobid = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    outorigin_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    outorigin_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    arriv_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    arriv_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    arriv_pic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pod_ret_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pod_ret_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    pod_ret_pic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pod_send_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pod_send_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    pod_send_pic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pod_status = table.Column<byte>(type: "tinyint", nullable: true),
                    spd_no = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    pod_remark = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_JOB_POD", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ORDER",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wh_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sub_custid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cnee_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dest_area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    tot_pkgs = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    uom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    pallet_consume = table.Column<int>(type: "int", nullable: true),
                    pallet_delivery = table.Column<int>(type: "int", nullable: true),
                    si_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    do_rcv_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    do_rcv_time = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    moda_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    serv_req = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    truck_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    order_status = table.Column<byte>(type: "tinyint", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ORDER", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ORDER_DTL",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_seq_order = table.Column<int>(type: "int", nullable: false),
                    wh_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sub_custid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cnee_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    inv_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    gi_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pu_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    item_category = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    pkg_unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    item_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    item_length = table.Column<int>(type: "int", nullable: true),
                    item_width = table.Column<int>(type: "int", nullable: true),
                    item_height = table.Column<int>(type: "int", nullable: true),
                    item_wgt = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    pack_unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    pallet_qty = table.Column<int>(type: "int", nullable: true),
                    koli_qty = table.Column<int>(type: "int", nullable: true),
                    full_addres = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ORDER_DTL", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ORIGIN",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origin_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    origin_loccode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ORIGIN", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "TRC_SERV_MODA",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    moda_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_SERV_MODA", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_SERV_TYPE",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    serv_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_SERV_TYPE", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_TRUCKSIZE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    trucksize_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_TRUCKSIZE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_VENDOR",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SUP_CODE = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    SUP_TYPE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SUP_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_ADDR1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_CITY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_EMAIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SUP_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SUP_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACTIVE_FLAG = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_VENDOR", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_VENDOR_TRUCK",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sup_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vehicle_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    vehicle_merk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_doortype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_driver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_STNK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_STNK_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_KIR = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_KIR_exp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_emisi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    vehicle_active = table.Column<byte>(type: "tinyint", nullable: true),
                    vehicle_remark = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    vehicle_KTP = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    vehicle_SIM = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_VENDOR_TRUCK", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_WH",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wh_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    wh_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_WH", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CUSTOMER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CUST_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CUST_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CUST_ADDR1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_ADDR2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_CITY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_EMAIL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_TEL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_FAX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_PIC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACTIVE_FLAG = table.Column<int>(type: "int", nullable: false),
                    MAIN_CUST = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    CUST_CUTOFF = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUSTOMER", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TRC_CUSTOMER_TRC_CUSTOMER_MAIN_MAIN_CUST",
                        column: x => x.MAIN_CUST,
                        principalTable: "TRC_CUSTOMER_MAIN",
                        principalColumn: "MAIN_CUST",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_CUSTOMER_MAIN_CUST",
                table: "TRC_CUSTOMER",
                column: "MAIN_CUST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_AREA_GROUP");

            migrationBuilder.DropTable(
                name: "TRC_CHARGE_UOM");

            migrationBuilder.DropTable(
                name: "TRC_CONSIGNEE");

            migrationBuilder.DropTable(
                name: "TRC_CUST_GROUP");

            migrationBuilder.DropTable(
                name: "TRC_CUSTOMER");

            migrationBuilder.DropTable(
                name: "TRC_DESTINATION");

            migrationBuilder.DropTable(
                name: "TRC_DRIVER");

            migrationBuilder.DropTable(
                name: "TRC_JOB");

            migrationBuilder.DropTable(
                name: "TRC_JOB_POD");

            migrationBuilder.DropTable(
                name: "TRC_ORDER");

            migrationBuilder.DropTable(
                name: "TRC_ORDER_DTL");

            migrationBuilder.DropTable(
                name: "TRC_ORIGIN");

            migrationBuilder.DropTable(
                name: "TRC_PRICEBUY");

            migrationBuilder.DropTable(
                name: "TRC_PRICESELL");

            migrationBuilder.DropTable(
                name: "TRC_SERV_MODA");

            migrationBuilder.DropTable(
                name: "TRC_SERV_TYPE");

            migrationBuilder.DropTable(
                name: "TRC_TRUCKSIZE");

            migrationBuilder.DropTable(
                name: "TRC_VENDOR");

            migrationBuilder.DropTable(
                name: "TRC_VENDOR_TRUCK");

            migrationBuilder.DropTable(
                name: "TRC_WH");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "TRC_CUSTOMER_MAIN");
        }
    }
}
