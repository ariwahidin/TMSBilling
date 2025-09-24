using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddIsUpldarOrderMceasy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_job_id",
                table: "TRC_JOB");

            migrationBuilder.AddColumn<bool>(
                name: "mceasy_is_upload",
                table: "TRC_ORDER",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_is_upload",
                table: "TRC_ORDER");

            migrationBuilder.AddColumn<string>(
                name: "mceasy_job_id",
                table: "TRC_JOB",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
