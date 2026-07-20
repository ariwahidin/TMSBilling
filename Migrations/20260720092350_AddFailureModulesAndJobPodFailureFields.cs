using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddFailureModulesAndJobPodFailureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failure_id",
                table: "TRC_JOB_POD",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failure_type_id",
                table: "TRC_JOB_POD",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failure_id",
                table: "TRC_JOB_POD");

            migrationBuilder.DropColumn(
                name: "failure_type_id",
                table: "TRC_JOB_POD");
        }
    }
}
