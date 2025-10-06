using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
	/// <inheritdoc />
	public partial class FirstPerformanceCalculation : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Delete all existing currency exchange rates from the market data table
			migrationBuilder.Sql("DELETE FROM MarketData WHERE SymbolProfileSymbol IN (" +
									"SELECT distinct SymbolProfileSymbol " +
									"FROM MarketData " +
									"WHERE length(SymbolProfileSymbol) = 6 " +
									"AND CURRENCYCLOSE = SUBSTR(SymbolProfileSymbol, 0, 4))");

			migrationBuilder.CreateTable(
				name: "CurrencyExchangeProfile",
				columns: table => new
				{
					ID = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					SourceCurrency = table.Column<string>(type: "TEXT", nullable: false),
					TargetCurrency = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CurrencyExchangeProfile", x => x.ID);
				});

			migrationBuilder.CreateTable(
				name: "HoldingAggregateds",
				columns: table => new
				{
					Id = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Symbol = table.Column<string>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", nullable: true),
					DataSource = table.Column<string>(type: "TEXT", nullable: false),
					AssetClass = table.Column<string>(type: "TEXT", nullable: false),
					AssetSubClass = table.Column<string>(type: "TEXT", nullable: true),
					ActivityCount = table.Column<int>(type: "INTEGER", nullable: false),
					CountryWeight = table.Column<string>(type: "TEXT", nullable: false),
					SectorWeights = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_HoldingAggregateds", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "CurrencyExchangeRate",
				columns: table => new
				{
					ID = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					TradingVolume = table.Column<decimal>(type: "TEXT", nullable: false),
					Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
					CurrencyExchangeProfileID = table.Column<long>(type: "INTEGER", nullable: true),
					Close = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyClose = table.Column<string>(type: "TEXT", nullable: false),
					High = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyHigh = table.Column<string>(type: "TEXT", nullable: false),
					Low = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyLow = table.Column<string>(type: "TEXT", nullable: false),
					Open = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyOpen = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CurrencyExchangeRate", x => x.ID);
					table.ForeignKey(
						name: "FK_CurrencyExchangeRate_CurrencyExchangeProfile_CurrencyExchangeProfileID",
						column: x => x.CurrencyExchangeProfileID,
						principalTable: "CurrencyExchangeProfile",
						principalColumn: "ID",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "CalculatedSnapshots",
				columns: table => new
				{
					Id = table.Column<long>(type: "integer", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
					Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
					HoldingAggregatedId = table.Column<long>(type: "INTEGER", nullable: false),
					AverageCostPrice = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyAverageCostPrice = table.Column<string>(type: "TEXT", nullable: false),
					CurrentUnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyCurrentUnitPrice = table.Column<string>(type: "TEXT", nullable: false),
					TotalInvested = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyTotalInvested = table.Column<string>(type: "TEXT", nullable: false),
					TotalValue = table.Column<decimal>(type: "TEXT", nullable: false),
					CurrencyTotalValue = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CalculatedSnapshots", x => x.Id);
					table.ForeignKey(
						name: "FK_CalculatedSnapshots_HoldingAggregateds_HoldingAggregatedId",
						column: x => x.HoldingAggregatedId,
						principalTable: "HoldingAggregateds",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_CalculatedSnapshots_HoldingAggregatedId",
				table: "CalculatedSnapshots",
				column: "HoldingAggregatedId");

			migrationBuilder.CreateIndex(
				name: "IX_CurrencyExchangeProfile_SourceCurrency_TargetCurrency",
				table: "CurrencyExchangeProfile",
				columns: new[] { "SourceCurrency", "TargetCurrency" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_CurrencyExchangeRate_CurrencyExchangeProfileID_Date",
				table: "CurrencyExchangeRate",
				columns: new[] { "CurrencyExchangeProfileID", "Date" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "CalculatedSnapshots");

			migrationBuilder.DropTable(
				name: "CurrencyExchangeRate");

			migrationBuilder.DropTable(
				name: "HoldingAggregateds");

			migrationBuilder.DropTable(
				name: "CurrencyExchangeProfile");
		}
	}
}
