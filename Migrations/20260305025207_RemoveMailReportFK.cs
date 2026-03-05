using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMailReportFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RPT_EXCEL_LAYOUT_RPT_MAIL_REPORT_report_id",
                table: "RPT_EXCEL_LAYOUT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_RPT_EXCEL_LAYOUT_RPT_MAIL_REPORT_report_id",
                table: "RPT_EXCEL_LAYOUT",
                column: "report_id",
                principalTable: "RPT_MAIL_REPORT",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
