using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTableFleetOrderMcEasyAddshipment_reference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "reference_number",
                table: "MC_FO",
                newName: "shipment_reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "shipment_reference",
                table: "MC_FO",
                newName: "reference_number");
        }
    }
}
