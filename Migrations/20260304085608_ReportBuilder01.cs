using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ReportBuilder01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.AddColumn<string>(
                name: "owner_type",
                table: "RPT_EXCEL_LAYOUT",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT",
                columns: new[] { "report_id", "owner_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id_owner_type",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.DropColumn(
                name: "owner_type",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id",
                table: "RPT_EXCEL_LAYOUT",
                column: "report_id",
                unique: true);
        }
    }
}
