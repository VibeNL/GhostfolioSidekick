using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class Dividends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "SymbolProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UpcomingDividends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExDividendDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DividendType = table.Column<int>(type: "INTEGER", nullable: false),
                    DividendState = table.Column<int>(type: "INTEGER", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyAmount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpcomingDividends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpcomingDividends_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_UpcomingDividends_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "UpcomingDividends",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpcomingDividends");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "SymbolProfiles");
        }
    }
}
