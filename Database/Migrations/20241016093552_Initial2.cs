using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartialSymbolIdentifiers");

            migrationBuilder.CreateTable(
                name: "ActivitySymbol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySymbol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivitySymbol_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivitySymbol_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                        columns: x => new { x.SymbolProfileSymbol, x.SymbolProfileDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySymbol_ActivityId",
                table: "ActivitySymbol",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySymbol_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "ActivitySymbol",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivitySymbol");

            migrationBuilder.CreateTable(
                name: "PartialSymbolIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AllowedAssetClasses = table.Column<string>(type: "TEXT", nullable: true),
                    AllowedAssetSubClasses = table.Column<string>(type: "TEXT", nullable: true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialSymbolIdentifiers", x => x.Id);
                });
        }
    }
}
