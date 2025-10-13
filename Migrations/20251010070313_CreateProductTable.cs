using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMSBilling.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TRC_PRODUCT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PRODUCT_ID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PRODUCT_TYPE_ID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PRODUCT_TYPE_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PRODUCT_CATEGORY_ID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PRODUCT_CATEGORY_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DESCRIPTION = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UOM = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WIDTH = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LENGTH = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HEIGHT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CBM = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WEIGHT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VOLUME = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PRICE = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CREATED_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UPDATED_BY = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRC_PRODUCT", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRC_PRODUCT");
        }
    }
}
