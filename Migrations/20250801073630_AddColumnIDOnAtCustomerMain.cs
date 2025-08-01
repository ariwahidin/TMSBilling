using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnIDOnAtCustomerMain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TRC_CUSTOMER_TRC_CUSTOMER_MAIN_MAIN_CUST",
                table: "TRC_CUSTOMER");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TRC_CUSTOMER_MAIN",
                table: "TRC_CUSTOMER_MAIN");

            migrationBuilder.DropIndex(
                name: "IX_TRC_CUSTOMER_MAIN_CUST",
                table: "TRC_CUSTOMER");

            migrationBuilder.AddColumn<int>(
                name: "ID",
                table: "TRC_CUSTOMER_MAIN",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "MAIN_CUST",
                table: "TRC_CUSTOMER",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRC_CUSTOMER_MAIN",
                table: "TRC_CUSTOMER_MAIN",
                column: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TRC_CUSTOMER_MAIN",
                table: "TRC_CUSTOMER_MAIN");

            migrationBuilder.DropColumn(
                name: "ID",
                table: "TRC_CUSTOMER_MAIN");

            migrationBuilder.AlterColumn<string>(
                name: "MAIN_CUST",
                table: "TRC_CUSTOMER",
                type: "nvarchar(30)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRC_CUSTOMER_MAIN",
                table: "TRC_CUSTOMER_MAIN",
                column: "MAIN_CUST");

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
    }
}
