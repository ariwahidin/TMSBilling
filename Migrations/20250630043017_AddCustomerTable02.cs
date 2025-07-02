using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerTable02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_CUST_GROUP",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SUB_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CUST_CODE = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUST_GROUP", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TRC_CUSTOMER_MAIN",
                columns: table => new
                {
                    MAIN_CUST = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CUST_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_ADDR1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_CITY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_EMAIL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CUST_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CUST_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CUST_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TAX_IMG = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STATUS_FLAG = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CUSTOMER_MAIN", x => x.MAIN_CUST);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_CUST_GROUP");

            migrationBuilder.DropTable(
                name: "TRC_CUSTOMER_MAIN");
        }
    }
}
