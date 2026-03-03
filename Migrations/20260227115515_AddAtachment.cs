using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddAtachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAttachmentMode",
                table: "Integrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailFooterTemplate",
                table: "Integrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailIncludeDataTable",
                table: "Integrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IntegrationAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationId = table.Column<int>(type: "int", nullable: false),
                    AttachmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GroupFileKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SheetOrder = table.Column<int>(type: "int", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationAttachments_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAttachments_IntegrationId",
                table: "IntegrationAttachments",
                column: "IntegrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationAttachments");

            migrationBuilder.DropColumn(
                name: "EmailAttachmentMode",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "EmailFooterTemplate",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "EmailIncludeDataTable",
                table: "Integrations");
        }
    }
}
