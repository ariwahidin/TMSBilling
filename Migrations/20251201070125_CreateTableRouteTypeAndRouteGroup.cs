using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableRouteTypeAndRouteGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_GROUP",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    dest = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_GROUP", x => x.id_seq);
                });

            migrationBuilder.CreateTable(
                name: "TRC_ROUTE_TYPE",
                columns: table => new
                {
                    id_seq = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    route_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entry_user = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_ROUTE_TYPE", x => x.id_seq);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_ROUTE_GROUP");

            migrationBuilder.DropTable(
                name: "TRC_ROUTE_TYPE");
        }
    }
}
