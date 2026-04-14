using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererNotOwnedTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IStockPriceRepository[] stockPriceRepositories)
		: MarketDataGathererTaskBase(databaseContextFactory, stockPriceRepositories)
	{
		public override TimeSpan ExecutionFrequency => Frequencies.Daily;

		public override string Name => "Market Data Gatherer (Not Owned)";

		protected override bool ShouldProcess(bool isCurrentlyOwned) => !isCurrentlyOwned;
	}
}
