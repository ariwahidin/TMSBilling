using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_JOB");
        }
    }
}
