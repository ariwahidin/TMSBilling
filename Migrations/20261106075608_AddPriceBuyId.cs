using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceBuyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PRICE_BUY_ID",
                table: "TRC_JOB",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PRICE_SELL_ID",
                table: "TRC_JOB",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PRICE_BUY_ID",
                table: "TRC_JOB");

            migrationBuilder.DropColumn(
                name: "PRICE_SELL_ID",
                table: "TRC_JOB");
        }
    }
}
