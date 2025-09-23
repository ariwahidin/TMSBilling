using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumMcEasyOrderIDAndDONumberInOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mceasy_do_number",
                table: "TRC_ORDER",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mceasy_order_id",
                table: "TRC_ORDER",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_do_number",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "mceasy_order_id",
                table: "TRC_ORDER");
        }
    }
}
