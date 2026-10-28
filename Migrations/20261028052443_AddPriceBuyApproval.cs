using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceBuyApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_PRICEBUY_APPROVAL_AUDIT",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICEBUY_APPROVAL_AUDIT", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "TRC_PRICEBUY_APPROVAL_H",
                columns: table => new
                {
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Approver = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICEBUY_APPROVAL_H", x => x.ApprovalId);
                });

            migrationBuilder.CreateTable(
                name: "TRC_PRICEBUY_APPROVAL_D",
                columns: table => new
                {
                    DetailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalId = table.Column<long>(type: "bigint", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PriceBuyId = table.Column<int>(type: "int", nullable: true),
                    ConflictKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PendingConflictKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRICEBUY_APPROVAL_D", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_TRC_PRICEBUY_APPROVAL_D_TRC_PRICEBUY_APPROVAL_H_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "TRC_PRICEBUY_APPROVAL_H",
                        principalColumn: "ApprovalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICEBUY_APPROVAL_D_ApprovalId",
                table: "TRC_PRICEBUY_APPROVAL_D",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICEBUY_APPROVAL_D_PendingConflictKey",
                table: "TRC_PRICEBUY_APPROVAL_D",
                column: "PendingConflictKey",
                unique: true,
                filter: "[PendingConflictKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TRC_PRICEBUY_APPROVAL_H_RequestNo",
                table: "TRC_PRICEBUY_APPROVAL_H",
                column: "RequestNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_PRICEBUY_APPROVAL_AUDIT");

            migrationBuilder.DropTable(
                name: "TRC_PRICEBUY_APPROVAL_D");

            migrationBuilder.DropTable(
                name: "TRC_PRICEBUY_APPROVAL_H");
        }
    }
}
