using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddCordinatesAndRadiusAtConsignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CORDINATES",
                table: "TRC_CONSIGNEE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RADIUS",
                table: "TRC_CONSIGNEE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CORDINATES",
                table: "TRC_CONSIGNEE");

            migrationBuilder.DropColumn(
                name: "RADIUS",
                table: "TRC_CONSIGNEE");
        }
    }
}
