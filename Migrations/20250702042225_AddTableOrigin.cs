using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddTableOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_ORIGIN",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origin_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entryuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entrydate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updateuser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updatedate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    origin_loccode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ORIGIN", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_ORIGIN");
        }
    }
}
