using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class DeterminePrice(IMarketDataService marketDataService) : IHoldingStrategy
	{
		public int Priority => (int)CryptoStrategiesPriority.DeterminePrice;

		public Task Execute(Holding holding)
		{
			//// TODO
			var activities = holding.Activities.OrderBy(x => x.Date).ToList();

			foreach (var activity in activities.Where(x => x.UnitPrice.Amount == 0))
			{
				activity.UnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
			}

			return Task.CompletedTask;
		}
		
		private async Task<Money> GetUnitPrice(SymbolProfile? symbolProfile, DateTime date)
		{
			var md = (await marketDataService.GetAllSymbolProfiles()).ToList();
			var profile = md.Single(x => x.Equals(symbolProfile));
			var marketDate = profile.MarketData.Single(x => x.Date == date);
			return new Money(symbolProfile!.Currency, marketDate.MarketPrice);
		}
	}
}
