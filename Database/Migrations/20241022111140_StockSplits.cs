using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class StockSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockSplits",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BeforeSplit = table.Column<decimal>(type: "TEXT", nullable: false),
                    AfterSplit = table.Column<decimal>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSplits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StockSplits_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockSplits_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "StockSplits",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockSplits");
        }
    }
}
