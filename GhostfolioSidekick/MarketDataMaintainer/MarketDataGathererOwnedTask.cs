using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererOwnedTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IStockPriceRepository[] stockPriceRepositories)
		: MarketDataGathererTaskBase(databaseContextFactory, stockPriceRepositories)
	{
		public override TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public override string Name => "Market Data Gatherer (Owned)";

		public override TaskPriority Priority => TaskPriority.MarketDataGatherer;

		protected override bool ShouldProcess(bool isCurrentlyOwned) => isCurrentlyOwned;
	}
}
