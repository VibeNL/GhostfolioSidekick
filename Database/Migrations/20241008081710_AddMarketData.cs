using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyClose = table.Column<string>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyOpen = table.Column<string>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyHigh = table.Column<string>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyLow = table.Column<string>(type: "TEXT", nullable: false),
                    TradingVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MarketData_SymbolProfiles_Symbol",
                        column: x => x.Symbol,
                        principalTable: "SymbolProfiles",
                        principalColumn: "Symbol");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Symbol",
                table: "MarketData",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketData");
        }
    }
}
