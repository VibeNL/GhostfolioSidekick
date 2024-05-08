using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class DeterminePrice : ApiStrategyBase, IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.DeterminePrice;

		public DeterminePrice(IMarketDataService marketDataService, ILogger<DeterminePrice> logger) : base(marketDataService, logger)
		{
		}

		public async Task Execute(Holding holding)
		{
			if (holding.SymbolProfile == null)
			{
				return;
			}

			var activities = holding.Activities
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
						sendAndReceiveActivity.UnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
						break;
					case GiftActivity giftActivity:
						giftActivity.UnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
						break;
					case StakingRewardActivity stakingRewardActivity:
						stakingRewardActivity.UnitPrice = await GetUnitPrice(holding.SymbolProfile, activity.Date);
						break;
					default:
						break;
				}
			}
		}
	}
}
