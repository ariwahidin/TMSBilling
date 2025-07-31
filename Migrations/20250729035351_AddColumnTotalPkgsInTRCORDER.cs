using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnTotalPkgsInTRCORDER : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_pkgs",
                table: "TRC_JOB");

            migrationBuilder.AddColumn<int>(
                name: "total_pkgs",
                table: "TRC_ORDER",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_pkgs",
                table: "TRC_ORDER");

            migrationBuilder.AddColumn<int>(
                name: "total_pkgs",
                table: "TRC_JOB",
                type: "int",
                nullable: true);
        }
    }
}
