using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class RequiredSymbolToBeSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			DeleteIncorrectData(migrationBuilder);

			migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileSymbol",
                table: "StockSplits",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileDataSource",
                table: "StockSplits",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileSymbol",
                table: "MarketData",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileDataSource",
                table: "MarketData",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileSymbol",
                table: "StockSplits",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileDataSource",
                table: "StockSplits",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileSymbol",
                table: "MarketData",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SymbolProfileDataSource",
                table: "MarketData",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

		private static void DeleteIncorrectData(MigrationBuilder migrationBuilder)
		{
			// Delete MarketData without symbol profile
			migrationBuilder.Sql(@"
				DELETE FROM MarketData
				WHERE SymbolProfileSymbol IS NULL OR SymbolProfileDataSource IS NULL;");

			// Delete StockSplits without symbol profile
			migrationBuilder.Sql(@"
				DELETE FROM StockSplits
				WHERE SymbolProfileSymbol IS NULL OR SymbolProfileDataSource IS NULL;");
		}
	}
}
