using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToCustomerMain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CC_EMAIL",
                table: "TRC_CUSTOMER_MAIN",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TO_EMAIL",
                table: "TRC_CUSTOMER_MAIN",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CC_EMAIL",
                table: "TRC_CUSTOMER_MAIN");

            migrationBuilder.DropColumn(
                name: "TO_EMAIL",
                table: "TRC_CUSTOMER_MAIN");
        }
    }
}
