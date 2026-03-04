using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ExcelDataLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RPT_EXCEL_LAYOUT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    use_custom_layout = table.Column<byte>(type: "tinyint", nullable: false),
                    report_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    title_bg_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    title_font_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    title_font_size = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_EXCEL_LAYOUT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_EXCEL_LAYOUT_RPT_MAIL_REPORT_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_MAIL_REPORT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_EXCEL_SHEET",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    layout_id = table.Column<int>(type: "int", nullable: false),
                    sheet_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_EXCEL_SHEET", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_EXCEL_SHEET_RPT_EXCEL_LAYOUT_layout_id",
                        column: x => x.layout_id,
                        principalTable: "RPT_EXCEL_LAYOUT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_EXCEL_SECTION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sheet_id = table.Column<int>(type: "int", nullable: false),
                    section_label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    sql_query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sql_where = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    visible_columns = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    layout = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    group_id = table.Column<int>(type: "int", nullable: false),
                    side_gap = table.Column<int>(type: "int", nullable: false),
                    bottom_gap = table.Column<int>(type: "int", nullable: false),
                    section_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    title_bg_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    title_font_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    header_bg_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    header_font_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    show_grand_total = table.Column<byte>(type: "tinyint", nullable: false),
                    grand_total_label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    total_bg_color = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    display_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_EXCEL_SECTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_EXCEL_SECTION_RPT_EXCEL_SHEET_sheet_id",
                        column: x => x.sheet_id,
                        principalTable: "RPT_EXCEL_SHEET",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_LAYOUT_report_id",
                table: "RPT_EXCEL_LAYOUT",
                column: "report_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_SECTION_sheet_id",
                table: "RPT_EXCEL_SECTION",
                column: "sheet_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_EXCEL_SHEET_layout_id",
                table: "RPT_EXCEL_SHEET",
                column: "layout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RPT_EXCEL_SECTION");

            migrationBuilder.DropTable(
                name: "RPT_EXCEL_SHEET");

            migrationBuilder.DropTable(
                name: "RPT_EXCEL_LAYOUT");
        }
    }
}
