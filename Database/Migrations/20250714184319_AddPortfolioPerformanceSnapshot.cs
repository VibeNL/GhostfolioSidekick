using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioPerformanceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioPerformanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortfolioHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", nullable: false),
                    CalculationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Performance_TimeWeightedReturn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_TotalDividends_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_TotalDividends_Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Performance_DividendYield = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_CurrencyImpact = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Performance_EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Performance_BaseCurrency = table.Column<string>(type: "TEXT", nullable: false),
                    Performance_InitialValue_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_InitialValue_Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Performance_FinalValue_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_FinalValue_Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Performance_NetCashFlows_Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Performance_NetCashFlows_Currency = table.Column<string>(type: "TEXT", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    ScopeIdentifier = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioPerformanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPerformanceSnapshot_CalculatedAt",
                table: "PortfolioPerformanceSnapshots",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPerformanceSnapshot_Latest",
                table: "PortfolioPerformanceSnapshots",
                columns: new[] { "StartDate", "EndDate", "BaseCurrency", "IsLatest", "Scope", "ScopeIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPerformanceSnapshot_Lookup",
                table: "PortfolioPerformanceSnapshots",
                columns: new[] { "PortfolioHash", "StartDate", "EndDate", "BaseCurrency", "CalculationType", "Scope", "ScopeIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPerformanceSnapshot_Scope",
                table: "PortfolioPerformanceSnapshots",
                columns: new[] { "Scope", "ScopeIdentifier" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioPerformanceSnapshots");
        }
    }
}
