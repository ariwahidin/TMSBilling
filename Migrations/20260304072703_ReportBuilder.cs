using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ReportBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RPT_DEFINITION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    report_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    report_desc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    allowed_outputs = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    default_output = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    filename_template = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    excel_layout_id = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<byte>(type: "tinyint", nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_DEFINITION", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RPT_PARAM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    param_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    param_label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    param_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    param_options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    options_source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    options_value_col = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    options_text_col = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    default_value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    placeholder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_required = table.Column<byte>(type: "tinyint", nullable: false),
                    is_hidden = table.Column<byte>(type: "tinyint", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    col_width = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_PARAM", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_PARAM_RPT_DEFINITION_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_DEFINITION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_PERMISSION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_PERMISSION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_PERMISSION_RPT_DEFINITION_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_DEFINITION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_RUN_LOG",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    run_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    params_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    output_format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    row_count = table.Column<int>(type: "int", nullable: true),
                    duration_ms = table.Column<int>(type: "int", nullable: true),
                    run_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_RUN_LOG", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_RUN_LOG_RPT_DEFINITION_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_DEFINITION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RPT_DEFINITION_category",
                table: "RPT_DEFINITION",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_DEFINITION_is_active",
                table: "RPT_DEFINITION",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_DEFINITION_report_code",
                table: "RPT_DEFINITION",
                column: "report_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RPT_PARAM_report_id",
                table: "RPT_PARAM",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_PERMISSION_report_id",
                table: "RPT_PERMISSION",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_PERMISSION_role_name",
                table: "RPT_PERMISSION",
                column: "role_name");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_RUN_LOG_report_id_run_at",
                table: "RPT_RUN_LOG",
                columns: new[] { "report_id", "run_at" });

            migrationBuilder.CreateIndex(
                name: "IX_RPT_RUN_LOG_run_by",
                table: "RPT_RUN_LOG",
                column: "run_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RPT_PARAM");

            migrationBuilder.DropTable(
                name: "RPT_PERMISSION");

            migrationBuilder.DropTable(
                name: "RPT_RUN_LOG");

            migrationBuilder.DropTable(
                name: "RPT_DEFINITION");
        }
    }
}
