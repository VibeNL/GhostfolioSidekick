using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class FirstPerformanceCalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoldingAggregateds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DataSource = table.Column<string>(type: "TEXT", nullable: false),
                    AssetClass = table.Column<string>(type: "TEXT", nullable: false),
                    AssetSubClass = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryWeight = table.Column<string>(type: "TEXT", nullable: false),
                    SectorWeights = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldingAggregateds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalculatedSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    HoldingAggregatedId = table.Column<long>(type: "INTEGER", nullable: false),
                    AverageCostPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyAverageCostPrice = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentUnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCurrentUnitPrice = table.Column<string>(type: "TEXT", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyTotalInvested = table.Column<string>(type: "TEXT", nullable: false),
                    TotalValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyTotalValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculatedSnapshots_HoldingAggregateds_HoldingAggregatedId",
                        column: x => x.HoldingAggregatedId,
                        principalTable: "HoldingAggregateds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedSnapshots_HoldingAggregatedId",
                table: "CalculatedSnapshots",
                column: "HoldingAggregatedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculatedSnapshots");

            migrationBuilder.DropTable(
                name: "HoldingAggregateds");
        }
    }
}
