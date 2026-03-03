using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChannelType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timing = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleFreq = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ScheduleHour = table.Column<int>(type: "int", nullable: true),
                    ScheduleMinute = table.Column<int>(type: "int", nullable: true),
                    ScheduleDayOfWeek = table.Column<int>(type: "int", nullable: true),
                    ScheduleDayOfMonth = table.Column<int>(type: "int", nullable: true),
                    SendEmailOnSuccess = table.Column<bool>(type: "bit", nullable: false),
                    SendEmailOnFailure = table.Column<bool>(type: "bit", nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailBodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationId = table.Column<int>(type: "int", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RemotePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApiMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ApiHeaders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileOutputPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileNameTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SpreadsheetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SheetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CredentialsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderRow = table.Column<int>(type: "int", nullable: true),
                    KeyColumn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduleMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationId = table.Column<int>(type: "int", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EventDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationHistories_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegrationId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationRecipients_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_IntegrationId",
                table: "IntegrationConnections",
                column: "IntegrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationHistories_IntegrationId",
                table: "IntegrationHistories",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRecipients_IntegrationId",
                table: "IntegrationRecipients",
                column: "IntegrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropTable(
                name: "IntegrationHistories");

            migrationBuilder.DropTable(
                name: "IntegrationRecipients");

            migrationBuilder.DropTable(
                name: "Integrations");
        }
    }
}
