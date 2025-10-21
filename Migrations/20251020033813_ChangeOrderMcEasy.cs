using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOrderMcEasy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fleet_task_id",
                table: "MC_ORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fleet_task_number",
                table: "MC_ORDER",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fleet_task_id",
                table: "MC_ORDER");

            migrationBuilder.DropColumn(
                name: "fleet_task_number",
                table: "MC_ORDER");
        }
    }
}
