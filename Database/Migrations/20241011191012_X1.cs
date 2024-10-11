using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class X1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClasses_AllowedAssetSubClasses",
                table: "PartialSymbolIdentifiers",
                columns: new[] { "Identifier", "AllowedAssetClasses", "AllowedAssetSubClasses" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClasses_AllowedAssetSubClasses",
                table: "PartialSymbolIdentifiers");
        }
    }
}
