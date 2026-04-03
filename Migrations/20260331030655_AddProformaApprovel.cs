using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddProformaApprovel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proforma_headers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    customer_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    periode_start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    periode_end = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approved_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proforma_headers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proforma_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    header_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proforma_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_proforma_details_proforma_headers_header_id",
                        column: x => x.header_id,
                        principalTable: "proforma_headers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_details_header_id",
                table: "proforma_details",
                column: "header_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proforma_details");

            migrationBuilder.DropTable(
                name: "proforma_headers");
        }
    }
}
