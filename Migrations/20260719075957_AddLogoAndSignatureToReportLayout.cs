using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoAndSignatureToReportLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logo_path",
                table: "RPT_EXCEL_LAYOUT",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signature_placement",
                table: "RPT_EXCEL_LAYOUT",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RPT_SIGNATURE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    layout_id = table.Column<int>(type: "int", nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPT_SIGNATURE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RPT_SIGNATURE_RPT_EXCEL_LAYOUT_layout_id",
                        column: x => x.layout_id,
                        principalTable: "RPT_EXCEL_LAYOUT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RPT_SIGNATURE_layout_id",
                table: "RPT_SIGNATURE",
                column: "layout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RPT_SIGNATURE");

            migrationBuilder.DropColumn(
                name: "logo_path",
                table: "RPT_EXCEL_LAYOUT");

            migrationBuilder.DropColumn(
                name: "signature_placement",
                table: "RPT_EXCEL_LAYOUT");
        }
    }
}
