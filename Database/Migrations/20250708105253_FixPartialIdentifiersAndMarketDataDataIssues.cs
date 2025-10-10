using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
	/// <inheritdoc />
	public partial class FixPartialIdentifiersAndMarketDataDataIssues : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			DeleteIncorrectData(migrationBuilder);

			migrationBuilder.CreateIndex(
				name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass",
				table: "PartialSymbolIdentifiers",
				columns: ["Identifier", "AllowedAssetClasses", "AllowedAssetSubClasses"],
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MarketData_SymbolProfileDataSource_SymbolProfileSymbol_Date",
				table: "MarketData",
				columns: ["SymbolProfileDataSource", "SymbolProfileSymbol", "Date"],
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_PartialSymbolIdentifiers_Identifier_AllowedAssetClass_AllowedAssetSubClass",
				table: "PartialSymbolIdentifiers");

			migrationBuilder.DropIndex(
				name: "IX_MarketData_SymbolProfileDataSource_SymbolProfileSymbol_Date",
				table: "MarketData");
		}

		private static void DeleteIncorrectData(MigrationBuilder migrationBuilder)
		{
			// Delete MarketData without symbol profile
			migrationBuilder.Sql(@"
				DELETE FROM MarketData
				WHERE SymbolProfileSymbol IS NULL OR SymbolProfileDataSource IS NULL;");

			// Delete duplicate PartialSymbolIdentifiers
			migrationBuilder.Sql(@"
				DELETE FROM PartialSymbolIdentifiers
				WHERE ID NOT IN (
					SELECT MIN(ID)
					FROM PartialSymbolIdentifiers
					GROUP BY Identifier, AllowedAssetClasses, AllowedAssetSubClasses
				);");
		}
	}
}
