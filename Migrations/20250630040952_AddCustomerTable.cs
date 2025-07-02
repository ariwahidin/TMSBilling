using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_CUSTOMER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CUST_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CUST_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CUST_ADDR1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_ADDR2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_CITY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_EMAIL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_TEL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_FAX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_PIC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACTIVE_FLAG = table.Column<int>(type: "int", nullable: false),
                    MAIN_CUST = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CUST_CUTOFF = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUSTOMER", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_CUSTOMER");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
