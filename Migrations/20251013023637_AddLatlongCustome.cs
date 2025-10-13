using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddLatlongCustome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CORDINATES",
                table: "TRC_GEOFENCE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LAT",
                table: "TRC_GEOFENCE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LONG",
                table: "TRC_GEOFENCE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RADIUS",
                table: "TRC_GEOFENCE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CORDINATES",
                table: "TRC_GEOFENCE");

            migrationBuilder.DropColumn(
                name: "LAT",
                table: "TRC_GEOFENCE");

            migrationBuilder.DropColumn(
                name: "LONG",
                table: "TRC_GEOFENCE");

            migrationBuilder.DropColumn(
                name: "RADIUS",
                table: "TRC_GEOFENCE");
        }
    }
}
