using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataGathererTask(IMarketDataRepository marketDataRepository, IStockPriceRepository stockPriceRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			var symbols = await marketDataRepository.GetSymbolProfiles();
			
			foreach (var symbol in symbols)
			{
				var date = await marketDataRepository.GetEarliestActivityDate(symbol);
				
				var minDate = DateOnly.FromDateTime(symbol.MarketData.Min(x => x.Date));
				var maxDate = DateOnly.FromDateTime(symbol.MarketData.Max(x => x.Date));	

				if (date >= minDate && DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) <= maxDate) // For now 1 day old only
				{
					continue;
				}

				var md = await stockPriceRepository.GetStockMarketData(symbol, date);

				symbol.MarketData.Clear();

				foreach (var marketData in md)
				{
					symbol.MarketData.Add(marketData);
				}

				await marketDataRepository.Store(symbol);
			}

		}
	}
}
