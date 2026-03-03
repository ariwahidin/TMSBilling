using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AdUserDefINOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_def_1",
                table: "TRC_ORDER_DTL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_def_2",
                table: "TRC_ORDER_DTL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_def_3",
                table: "TRC_ORDER_DTL",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_def_1",
                table: "TRC_ORDER_DTL");

            migrationBuilder.DropColumn(
                name: "user_def_2",
                table: "TRC_ORDER_DTL");

            migrationBuilder.DropColumn(
                name: "user_def_3",
                table: "TRC_ORDER_DTL");
        }
    }
}
