using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddReportMailer001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "grand_total_label",
                table: "RPT_MAIL_SECTION",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "show_grand_total",
                table: "RPT_MAIL_SECTION",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grand_total_label",
                table: "RPT_MAIL_SECTION");

            migrationBuilder.DropColumn(
                name: "show_grand_total",
                table: "RPT_MAIL_SECTION");
        }
    }
}
