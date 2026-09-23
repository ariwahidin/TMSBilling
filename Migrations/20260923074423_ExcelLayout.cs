using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ExcelLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attach_filename_query",
                table: "RPT_MAIL_TEMPLATE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report_title_query",
                table: "RPT_EXCEL_LAYOUT",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attach_filename_query",
                table: "RPT_MAIL_TEMPLATE");

            migrationBuilder.DropColumn(
                name: "report_title_query",
                table: "RPT_EXCEL_LAYOUT");
        }
    }
}
