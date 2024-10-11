using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class PartialIdentifiersFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartialSymbolIdentifiers_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.DropIndex(
                name: "IX_PartialSymbolIdentifiers_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.DropColumn(
                name: "SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.DropColumn(
                name: "SymbolProfileSymbol",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.AddColumn<int>(
                name: "PartialSymbolIdentifierId",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartialSymbolIdentifierSymbolProfile",
                columns: table => new
                {
                    MatchedPartialIdentifiersId = table.Column<int>(type: "INTEGER", nullable: false),
                    SymbolProfilesSymbol = table.Column<string>(type: "TEXT", nullable: false),
                    SymbolProfilesDataSource = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialSymbolIdentifierSymbolProfile", x => new { x.MatchedPartialIdentifiersId, x.SymbolProfilesSymbol, x.SymbolProfilesDataSource });
                    table.ForeignKey(
                        name: "FK_PartialSymbolIdentifierSymbolProfile_PartialSymbolIdentifiers_MatchedPartialIdentifiersId",
                        column: x => x.MatchedPartialIdentifiersId,
                        principalTable: "PartialSymbolIdentifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartialSymbolIdentifierSymbolProfile_SymbolProfiles_SymbolProfilesSymbol_SymbolProfilesDataSource",
                        columns: x => new { x.SymbolProfilesSymbol, x.SymbolProfilesDataSource },
                        principalTable: "SymbolProfiles",
                        principalColumns: new[] { "Symbol", "DataSource" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_PartialSymbolIdentifierId",
                table: "Activities",
                column: "PartialSymbolIdentifierId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifierSymbolProfile_SymbolProfilesSymbol_SymbolProfilesDataSource",
                table: "PartialSymbolIdentifierSymbolProfile",
                columns: new[] { "SymbolProfilesSymbol", "SymbolProfilesDataSource" });

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_PartialSymbolIdentifiers_PartialSymbolIdentifierId",
                table: "Activities",
                column: "PartialSymbolIdentifierId",
                principalTable: "PartialSymbolIdentifiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_PartialSymbolIdentifiers_PartialSymbolIdentifierId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "PartialSymbolIdentifierSymbolProfile");

            migrationBuilder.DropIndex(
                name: "IX_Activities_PartialSymbolIdentifierId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PartialSymbolIdentifierId",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SymbolProfileSymbol",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifiers_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" });

            migrationBuilder.AddForeignKey(
                name: "FK_PartialSymbolIdentifiers_SymbolProfiles_SymbolProfileSymbol_SymbolProfileDataSource",
                table: "PartialSymbolIdentifiers",
                columns: new[] { "SymbolProfileSymbol", "SymbolProfileDataSource" },
                principalTable: "SymbolProfiles",
                principalColumns: new[] { "Symbol", "DataSource" });
        }
    }
}
