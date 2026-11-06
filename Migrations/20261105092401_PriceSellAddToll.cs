using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class PriceSellAddToll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "rate_driver",
                table: "TRC_PRICESELL",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate_toll",
                table: "TRC_PRICESELL",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate_truck",
                table: "TRC_PRICESELL",
                type: "money",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rate_driver",
                table: "TRC_PRICESELL");

            migrationBuilder.DropColumn(
                name: "rate_toll",
                table: "TRC_PRICESELL");

            migrationBuilder.DropColumn(
                name: "rate_truck",
                table: "TRC_PRICESELL");
        }
    }
}
