using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_VENDOR",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SUP_CODE = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    SUP_TYPE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SUP_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_ADDR1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_CITY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_EMAIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SUP_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SUP_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SUP_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACTIVE_FLAG = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_VENDOR", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_VENDOR");
        }
    }
}
