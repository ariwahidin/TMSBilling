using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableJobPOD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_JOB_POD");
        }
    }
}
