using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldCustomerMainInDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MAIN_CUST",
                table: "TRC_DESTINATION",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MAIN_CUST",
                table: "TRC_DESTINATION");
        }
    }
}
