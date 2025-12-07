using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class UserCustomerChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserXCustomers_TRC_CUSTOMER_CustomerId",
                table: "UserXCustomers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserXCustomers_Users_UserId",
                table: "UserXCustomers");

            migrationBuilder.DropIndex(
                name: "IX_UserXCustomers_CustomerId",
                table: "UserXCustomers");

            migrationBuilder.DropIndex(
                name: "IX_UserXCustomers_UserId",
                table: "UserXCustomers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "UserXCustomers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserXCustomers");

            migrationBuilder.AddColumn<string>(
                name: "CustomerMain",
                table: "UserXCustomers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserXCustomers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerMain",
                table: "UserXCustomers");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserXCustomers");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "UserXCustomers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserXCustomers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserXCustomers_CustomerId",
                table: "UserXCustomers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserXCustomers_UserId",
                table: "UserXCustomers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserXCustomers_TRC_CUSTOMER_CustomerId",
                table: "UserXCustomers",
                column: "CustomerId",
                principalTable: "TRC_CUSTOMER",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserXCustomers_Users_UserId",
                table: "UserXCustomers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
