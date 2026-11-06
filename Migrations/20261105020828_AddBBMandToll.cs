using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddBBMandToll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "rate_bbm",
                table: "TRC_PRICEBUY",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rate_toll",
                table: "TRC_PRICEBUY",
                type: "money",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rate_bbm",
                table: "TRC_PRICEBUY");

            migrationBuilder.DropColumn(
                name: "rate_toll",
                table: "TRC_PRICEBUY");
        }
    }
}
