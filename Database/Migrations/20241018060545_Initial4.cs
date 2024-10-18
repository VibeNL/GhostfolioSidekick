using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryWeights");

            migrationBuilder.DropTable(
                name: "SectorWeights");

            migrationBuilder.AddColumn<string>(
                name: "CountryWeight",
                table: "SymbolProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SectorWeights",
                table: "SymbolProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryWeight",
                table: "SymbolProfiles");

            migrationBuilder.DropColumn(
                name: "SectorWeights",
                table: "SymbolProfiles");

            migrationBuilder.CreateTable(
                name: "CountryWeights",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Continent = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryWeights", x => x.Code);
                    table.ForeignKey(
                        name: "FK_CountryWeights_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" });
                });

            migrationBuilder.CreateTable(
                name: "SectorWeights",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorWeights", x => x.Name);
                    table.ForeignKey(
                        name: "FK_SectorWeights_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CountryWeights_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "CountryWeights",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });

            migrationBuilder.CreateIndex(
                name: "IX_SectorWeights_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "SectorWeights",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });
        }
    }
}
