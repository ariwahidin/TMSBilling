using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class Change_mceasy_dest_nameInOrderDt2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "origin_address",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_city",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_name",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ship_to_address",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ship_to_city",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ship_to_name",
                table: "TRC_ORDER",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "origin_address",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "origin_city",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "origin_name",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "ship_to_address",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "ship_to_city",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "ship_to_name",
                table: "TRC_ORDER");
        }
    }
}
