using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStockSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymbolProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", nullable: false),
                    AssetClass = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetSubClass = table.Column<int>(type: "INTEGER", nullable: true),
                    ISIN = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SymbolProfiles_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockSplitLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SymbolProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSplitLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockSplitLists_SymbolProfiles_SymbolProfileId",
                        column: x => x.SymbolProfileId,
                        principalTable: "SymbolProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SymbolProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    StockSplitListId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockSplits_StockSplitLists_StockSplitListId",
                        column: x => x.StockSplitListId,
                        principalTable: "StockSplitLists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockSplitLists_SymbolProfileId",
                table: "StockSplitLists",
                column: "SymbolProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockSplits_StockSplitListId",
                table: "StockSplits",
                column: "StockSplitListId");

            migrationBuilder.CreateIndex(
                name: "IX_StockSplits_SymbolProfileId_Date",
                table: "StockSplits",
                columns: new[] { "SymbolProfileId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SymbolProfiles_CurrencyId",
                table: "SymbolProfiles",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SymbolProfiles_Symbol",
                table: "SymbolProfiles",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockSplits");

            migrationBuilder.DropTable(
                name: "StockSplitLists");

            migrationBuilder.DropTable(
                name: "SymbolProfiles");

            migrationBuilder.DropTable(
                name: "Currencies");
        }
    }
}
