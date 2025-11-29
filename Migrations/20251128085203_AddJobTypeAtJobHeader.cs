using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTypeAtJobHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "job_type",
                table: "TRC_JOB_H",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "multitrip",
                table: "TRC_JOB_H",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_type",
                table: "TRC_JOB",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_type",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "multitrip",
                table: "TRC_JOB_H");

            migrationBuilder.DropColumn(
                name: "job_type",
                table: "TRC_JOB");
        }
    }
}
