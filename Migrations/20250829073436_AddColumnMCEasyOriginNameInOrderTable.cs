using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnMCEasyOriginNameInOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mceasy_dest_name",
                table: "TRC_ORDER",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mceasy_origin_name",
                table: "TRC_ORDER",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mceasy_dest_name",
                table: "TRC_ORDER");

            migrationBuilder.DropColumn(
                name: "mceasy_origin_name",
                table: "TRC_ORDER");
        }
    }
}
