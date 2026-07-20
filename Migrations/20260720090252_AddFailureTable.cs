using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddFailureTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_FAILURE_TYPE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    failure_type_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    failure_type_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_FAILURE_TYPE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TRC_FAILURE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    failure_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    failure_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    failure_description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    failure_type_id = table.Column<int>(type: "int", nullable: true),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_FAILURE", x => x.id);
                    table.ForeignKey(
                        name: "FK_TRC_FAILURE_TRC_FAILURE_TYPE_failure_type_id",
                        column: x => x.failure_type_id,
                        principalTable: "TRC_FAILURE_TYPE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_FAILURE_failure_type_id",
                table: "TRC_FAILURE",
                column: "failure_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_FAILURE");

            migrationBuilder.DropTable(
                name: "TRC_FAILURE_TYPE");
        }
    }
}
