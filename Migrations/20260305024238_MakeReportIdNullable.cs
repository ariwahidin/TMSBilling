using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class MakeReportIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.AlterColumn<int>(
                name: "report_id",
                table: "RPT_EXCEL_LAYOUT",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT",
                columns: new[] { "report_id", "owner_type" },
                unique: true,
                filter: "[report_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.AlterColumn<int>(
                name: "report_id",
                table: "RPT_EXCEL_LAYOUT",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT",
                columns: new[] { "report_id", "owner_type" },
                unique: true);
        }
    }
}
