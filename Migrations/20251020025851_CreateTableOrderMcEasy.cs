using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableOrderMcEasy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MC_ORDER",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shipment_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reference_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shipment_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MC_ORDER", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MC_ORDER");
        }
    }
}
