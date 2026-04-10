using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpcomingDividends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpcomingDividendTimelineEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HoldingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "TEXT", nullable: false),
                    AmountPrimaryCurrency = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    DividendType = table.Column<int>(type: "INTEGER", nullable: false),
                    DividendState = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpcomingDividendTimelineEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpcomingDividendTimelineEntries");
        }
    }
}
