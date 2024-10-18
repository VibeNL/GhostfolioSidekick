using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CoinGeckoAsset",
                schema: "cached",
                table: "CoinGeckoAsset");

            migrationBuilder.RenameTable(
                name: "CoinGeckoAsset",
                schema: "cached",
                newName: "CachedCoinGeckoAsset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CachedCoinGeckoAsset",
                table: "CachedCoinGeckoAsset",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CachedCoinGeckoAsset",
                table: "CachedCoinGeckoAsset");

            migrationBuilder.EnsureSchema(
                name: "cached");

            migrationBuilder.RenameTable(
                name: "CachedCoinGeckoAsset",
                newName: "CoinGeckoAsset",
                newSchema: "cached");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoinGeckoAsset",
                schema: "cached",
                table: "CoinGeckoAsset",
                column: "Id");
        }
    }
}
