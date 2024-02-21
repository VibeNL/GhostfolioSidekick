using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class DeterminePrice(IMarketDataService marketDataService) : IHoldingStrategy
	{
		public int Priority => (int)CryptoStrategiesPriority.DeterminePrice;

		public async Task Execute(Holding holding)
		{
			if (holding.SymbolProfile == null || holding.SymbolProfile.AssetSubClass != AssetSubClass.CryptoCurrency)
			{
				return;
			}

			var activities = holding.Activities
				.Where(x =>
					x.ActivityType == ActivityType.Send ||
					x.ActivityType == ActivityType.Receive)
				.OrderBy(x => x.Date).ToList();

			if (activities.Count == 0)
			{
				return;
			}

			foreach (var activity in activities.Where(x => (x.UnitPrice?.Amount ?? 0) == 0))
			{
				activity.UnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
			}
		}

		private async Task<Money> GetUnitPrice(SymbolProfile symbolProfile, DateTime date)
		{
			var marketDataProfile = await marketDataService.GetMarketData(symbolProfile.Symbol, symbolProfile.DataSource);
			var marketDate = marketDataProfile.MarketData.SingleOrDefault(x => x.Date.Date == date.Date);
			return new Money(symbolProfile!.Currency, marketDate?.MarketPrice.Amount ?? 0);
		}
	}
}
