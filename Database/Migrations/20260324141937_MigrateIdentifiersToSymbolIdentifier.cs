using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class MigrateIdentifiersToSymbolIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All pre-existing rows lack IdentifierType context and would receive the incorrect
            // default value of 0 (IdentifierType.Default). Since the correct type (ISIN, Ticker,
            // Name, …) cannot be inferred from the Identifier string alone, purge the stale rows.
            // The application will regenerate them with correct IdentifierType values on next run.
            // SQLite disables FK enforcement by default so the CASCADE on the join table will not
            // fire automatically — delete the join table first to avoid orphaned rows.
            migrationBuilder.Sql("DELETE FROM PartialSymbolIdentifierActivity");
            migrationBuilder.Sql("DELETE FROM PartialSymbolIdentifiers");

            migrationBuilder.DropIndex(
                name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAssetSubClasses",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAssetClasses",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentifierType",
                table: "PartialSymbolIdentifiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifiers_IdentifierType_Identifier_Currency_AllowedAssetClass_AllowedAssetSubClass",
                table: "PartialSymbolIdentifiers",
                columns: new[] { "IdentifierType", "Identifier", "Currency", "AllowedAssetClasses", "AllowedAssetSubClasses" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartialSymbolIdentifiers_IdentifierType_Identifier_Currency_AllowedAssetClass_AllowedAssetSubClass",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.DropColumn(
                name: "IdentifierType",
                table: "PartialSymbolIdentifiers");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAssetSubClasses",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAssetClasses",
                table: "PartialSymbolIdentifiers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass",
                table: "PartialSymbolIdentifiers",
                columns: new[] { "Identifier", "AllowedAssetClasses", "AllowedAssetSubClasses" },
                unique: true);
        }
    }
}
