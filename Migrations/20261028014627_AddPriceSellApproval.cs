using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSellApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_PRICESELL_APPROVAL",
                columns: table => new
                {
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICESELL_APPROVAL", x => x.ApprovalId);
                });

            migrationBuilder.CreateTable(
                name: "TRC_PRICESELL_AUDIT",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    ApprovalDetailId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICESELL_AUDIT", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "TRC_PRICESELL_APPROVAL_DETAIL",
                columns: table => new
                {
                    ApprovalDetailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PriceSellId = table.Column<int>(type: "int", nullable: true),
                    ConflictKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PendingConflictKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICESELL_APPROVAL_DETAIL", x => x.ApprovalDetailId);
                    table.ForeignKey(
                        name: "FK_TRC_PRICESELL_APPROVAL_DETAIL_TRC_PRICESELL_APPROVAL_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "TRC_PRICESELL_APPROVAL",
                        principalColumn: "ApprovalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICESELL_APPROVAL_RequestNo",
                table: "TRC_PRICESELL_APPROVAL",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICESELL_APPROVAL_DETAIL_ApprovalId",
                table: "TRC_PRICESELL_APPROVAL_DETAIL",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICESELL_APPROVAL_DETAIL_PendingConflictKey",
                table: "TRC_PRICESELL_APPROVAL_DETAIL",
                column: "PendingConflictKey",
                unique: true,
                filter: "[PendingConflictKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICESELL_AUDIT_ApprovalId_PerformedAt",
                table: "TRC_PRICESELL_AUDIT",
                columns: new[] { "ApprovalId", "PerformedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_PRICESELL_APPROVAL_DETAIL");

            migrationBuilder.DropTable(
                name: "TRC_PRICESELL_AUDIT");

            migrationBuilder.DropTable(
                name: "TRC_PRICESELL_APPROVAL");
        }
    }
}
