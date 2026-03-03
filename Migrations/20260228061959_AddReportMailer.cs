using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddReportMailer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RPT_MAIL_REPORT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    report_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    report_desc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    trigger_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    event_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    schedule_freq = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    schedule_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    schedule_day_of_week = table.Column<int>(type: "int", nullable: true),
                    schedule_day_of_month = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<byte>(type: "tinyint", nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_MAIL_REPORT", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RPT_MAIL_LOG",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    trigger_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    trigger_ref = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    recipients_to = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    recipients_cc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subject_sent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    body_sent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    triggered_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_MAIL_LOG", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_MAIL_LOG_RPT_MAIL_REPORT_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_MAIL_REPORT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_MAIL_RECIPIENT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    recipient_source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    truck_email_id = table.Column<int>(type: "int", nullable: true),
                    email_address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email_type = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    is_active = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_MAIL_RECIPIENT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_MAIL_RECIPIENT_RPT_MAIL_REPORT_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_MAIL_REPORT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RPT_MAIL_RECIPIENT_TRC_VENDOR_TRUCK_EMAIL_truck_email_id",
                        column: x => x.truck_email_id,
                        principalTable: "TRC_VENDOR_TRUCK_EMAIL",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "RPT_MAIL_SECTION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    section_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    section_label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    sql_query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sql_where = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    display_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    visible_columns = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    use_as_placeholder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_MAIL_SECTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_MAIL_SECTION_RPT_MAIL_REPORT_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_MAIL_REPORT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RPT_MAIL_TEMPLATE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    body_header = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    body_footer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attach_pdf = table.Column<byte>(type: "tinyint", nullable: false),
                    attach_excel = table.Column<byte>(type: "tinyint", nullable: false),
                    attach_filename = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_MAIL_TEMPLATE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_MAIL_TEMPLATE_RPT_MAIL_REPORT_report_id",
                        column: x => x.report_id,
                        principalTable: "RPT_MAIL_REPORT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RPT_MAIL_LOG_report_id",
                table: "RPT_MAIL_LOG",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_MAIL_RECIPIENT_report_id",
                table: "RPT_MAIL_RECIPIENT",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_MAIL_RECIPIENT_truck_email_id",
                table: "RPT_MAIL_RECIPIENT",
                column: "truck_email_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_MAIL_SECTION_report_id",
                table: "RPT_MAIL_SECTION",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_RPT_MAIL_TEMPLATE_report_id",
                table: "RPT_MAIL_TEMPLATE",
                column: "report_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RPT_MAIL_LOG");

            migrationBuilder.DropTable(
                name: "RPT_MAIL_RECIPIENT");

            migrationBuilder.DropTable(
                name: "RPT_MAIL_SECTION");

            migrationBuilder.DropTable(
                name: "RPT_MAIL_TEMPLATE");

            migrationBuilder.DropTable(
                name: "RPT_MAIL_REPORT");
        }
    }
}
