using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadTimeD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lead_time_hours",
                table: "TRC_LEADTIME",
                newName: "pod_days");

            migrationBuilder.RenameColumn(
                name: "lead_time_days",
                table: "TRC_LEADTIME",
                newName: "delivery_days");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pod_days",
                table: "TRC_LEADTIME",
                newName: "lead_time_hours");

            migrationBuilder.RenameColumn(
                name: "delivery_days",
                table: "TRC_LEADTIME",
                newName: "lead_time_days");
        }
    }
}
