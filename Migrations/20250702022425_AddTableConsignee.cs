using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddTableConsignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MAIN_CUST",
                table: "TRC_CUSTOMER",
                type: "nvarchar(30)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "TRC_CONSIGNEE",
                columns: table => new
                {
                    CNEE_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CNEE_NAME = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR1 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CNEE_ADDR2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_ADDR4 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNEE_TEL = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CNEE_FAX = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CNEE_PIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TAX_REG_NO = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ACTIVE_FLAG = table.Column<byte>(type: "tinyint", nullable: true),
                    SUB_CODE = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ENTRY_USER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ENTRY_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATE_USER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UPDATE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_CONSIGNEE", x => x.CNEE_CODE);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_CUSTOMER_MAIN_CUST",
                table: "TRC_CUSTOMER",
                column: "MAIN_CUST");

            migrationBuilder.AddForeignKey(
                name: "FK_TRC_CUSTOMER_TRC_CUSTOMER_MAIN_MAIN_CUST",
                table: "TRC_CUSTOMER",
                column: "MAIN_CUST",
                principalTable: "TRC_CUSTOMER_MAIN",
                principalColumn: "MAIN_CUST",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TRC_CUSTOMER_TRC_CUSTOMER_MAIN_MAIN_CUST",
                table: "TRC_CUSTOMER");

            migrationBuilder.DropTable(
                name: "TRC_CONSIGNEE");

            migrationBuilder.DropIndex(
                name: "IX_TRC_CUSTOMER_MAIN_CUST",
                table: "TRC_CUSTOMER");

            migrationBuilder.AlterColumn<string>(
                name: "MAIN_CUST",
                table: "TRC_CUSTOMER",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)");
        }
    }
}
