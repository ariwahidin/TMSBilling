using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingTypeInOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "packing_type",
                table: "TRC_ORDER",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "packing_type",
                table: "TRC_ORDER");
        }
    }
}
