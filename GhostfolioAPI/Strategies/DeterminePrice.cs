using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Strategies;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class DeterminePrice(IMarketDataService marketDataService) : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.DeterminePrice;

		public async Task Execute(Holding holding)
		{
			if (holding.SymbolProfile == null)
			{
				return;
			}

			var activities = holding.Activities
				.Where(x => x is SendAndReceiveActivity || x is GiftActivity)
				.OrderBy(x => x.Date).ToList();

			if (activities.Count == 0)
			{
				return;
			}

			foreach (var activity in activities)
			{
				switch (activity)
				{
					case SendAndReceiveActivity sendAndReceiveActivity:
						sendAndReceiveActivity.CalculatedUnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
						break;
					case GiftActivity giftActivity:
						giftActivity.CalculatedUnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
						break;
					default:
						throw new NotSupportedException($"Activity type {activity.GetType()} is not supported.");
				}
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
