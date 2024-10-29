using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReintroduceHolding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivitySymbol");

            migrationBuilder.AddColumn<int>(
                name: "HoldingId",
                table: "SymbolProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldingId",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Holdings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holdings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SymbolProfiles_HoldingId",
                table: "SymbolProfiles",
                column: "HoldingId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_HoldingId",
                table: "Activities",
                column: "HoldingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Holdings_HoldingId",
                table: "Activities",
                column: "HoldingId",
                principalTable: "Holdings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SymbolProfiles_Holdings_HoldingId",
                table: "SymbolProfiles",
                column: "HoldingId",
                principalTable: "Holdings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Holdings_HoldingId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_SymbolProfiles_Holdings_HoldingId",
                table: "SymbolProfiles");

            migrationBuilder.DropTable(
                name: "Holdings");

            migrationBuilder.DropIndex(
                name: "IX_SymbolProfiles_HoldingId",
                table: "SymbolProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Activities_HoldingId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "HoldingId",
                table: "SymbolProfiles");

            migrationBuilder.DropColumn(
                name: "HoldingId",
                table: "Activities");

            migrationBuilder.CreateTable(
                name: "ActivitySymbol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityId = table.Column<long>(type: "INTEGER", nullable: true),
                    SymbolProfileSymbol = table.Column<string>(type: "TEXT", nullable: true),
                    SymbolProfileDataSource = table.Column<string>(type: "TEXT", nullable: true),
                    PartialSymbolIdentifiers = table.Column<string>(type: "TEXT", nullable: false)
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
    }
}
