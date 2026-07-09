using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceTargetEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HighestTargetPriceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageTargetPriceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LowestTargetPriceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfBuys = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfHolds = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfSells = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageTargetPrice = table.Column<string>(type: "TEXT", nullable: false),
                    HighestTargetPrice = table.Column<string>(type: "TEXT", nullable: false),
                    LowestTargetPrice = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceTargets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceTargets_Symbol",
                table: "PriceTargets",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceTargets");
        }
    }
}
