using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaInOriginAdnDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "area",
                table: "TRC_ORIGIN",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "area",
                table: "TRC_DESTINATION",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AREA",
                table: "TRC_CONSIGNEE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CATEGORY",
                table: "TRC_CONSIGNEE",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CITY",
                table: "TRC_CONSIGNEE",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "area",
                table: "TRC_ORIGIN");

            migrationBuilder.DropColumn(
                name: "area",
                table: "TRC_DESTINATION");

            migrationBuilder.DropColumn(
                name: "AREA",
                table: "TRC_CONSIGNEE");

            migrationBuilder.DropColumn(
                name: "CATEGORY",
                table: "TRC_CONSIGNEE");

            migrationBuilder.DropColumn(
                name: "CITY",
                table: "TRC_CONSIGNEE");
        }
    }
}
