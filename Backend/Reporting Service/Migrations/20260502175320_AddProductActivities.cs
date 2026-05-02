using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportingService.Migrations
{
    /// <inheritdoc />
    public partial class AddProductActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldStock = table.Column<int>(type: "int", nullable: true),
                    NewStock = table.Column<int>(type: "int", nullable: true),
                    MediaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductActivities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductActivities");
        }
    }
}
