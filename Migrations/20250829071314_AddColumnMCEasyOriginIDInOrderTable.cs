using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnMCEasyOriginIDInOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mceasy_destination_address_id",
                table: "TRC_ORDER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mceasy_origin_address_id",
                table: "TRC_ORDER",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_destination_address_id",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "mceasy_origin_address_id",
                table: "TRC_ORDER");
        }
    }
}
