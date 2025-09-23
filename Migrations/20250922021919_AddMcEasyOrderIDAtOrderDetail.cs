using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddMcEasyOrderIDAtOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mceasy_order_dtl_id",
                table: "TRC_ORDER_DTL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mceasy_order_id",
                table: "TRC_ORDER_DTL",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_order_dtl_id",
                table: "TRC_ORDER_DTL");

            migrationBuilder.DropColumn(
                name: "mceasy_order_id",
                table: "TRC_ORDER_DTL");
        }
    }
}
