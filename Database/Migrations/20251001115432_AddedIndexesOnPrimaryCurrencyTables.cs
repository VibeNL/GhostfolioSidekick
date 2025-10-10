using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
	/// <inheritdoc />
	public partial class AddedIndexesOnPrimaryCurrencyTables : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateIndex(
				name: "IX_CalculatedSnapshots_HoldingAggregatedId_Date",
				table: "CalculatedSnapshots",
				columns: ["HoldingAggregatedId", "Date"]);

			migrationBuilder.CreateIndex(
				name: "IX_CalculatedSnapshotPrimaryCurrencies_HoldingAggregatedId_Date",
				table: "CalculatedSnapshotPrimaryCurrencies",
				columns: ["HoldingAggregatedId", "Date"]);

			migrationBuilder.CreateIndex(
				name: "IX_BalancePrimaryCurrencies_Date",
				table: "BalancePrimaryCurrencies",
				column: "Date");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_CalculatedSnapshots_HoldingAggregatedId_Date",
				table: "CalculatedSnapshots");

			migrationBuilder.DropIndex(
				name: "IX_CalculatedSnapshotPrimaryCurrencies_HoldingAggregatedId_Date",
				table: "CalculatedSnapshotPrimaryCurrencies");

			migrationBuilder.DropIndex(
				name: "IX_BalancePrimaryCurrencies_Date",
				table: "BalancePrimaryCurrencies");
		}
	}
}
