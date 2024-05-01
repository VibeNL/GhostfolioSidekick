using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Strategies;

namespace GhostfolioSidekick.Cryptocurrency
{
	public class AddStakeRewardsToPreviousBuyActivity(Settings settings) : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.StakeRewardWorkaround;

		public Task Execute(Holding holding)
		{
			if (!settings.CryptoWorkaroundStakeReward || holding.SymbolProfile?.AssetSubClass != AssetSubClass.CryptoCurrency)
			{
				return Task.CompletedTask;
			}

			var activities = holding.Activities.OrderBy(x => x.Date).ToList();

			var stakeRewards = activities.OfType<StakingRewardActivity>().ToList();

			foreach (StakingRewardActivity stakeReward in stakeRewards)
			{
				var buyActivity = activities
					.OfType<BuySellActivity>()
					.LastOrDefault(x => x.Quantity > 0 &&
										x.Date < stakeReward.Date);

				if (buyActivity != null)
				{
					buyActivity.Quantity += stakeReward!.Quantity;

					if (buyActivity.UnitPrice != null)
					{
						buyActivity.UnitPrice.Amount = buyActivity.UnitPrice.Amount * (buyActivity.Quantity / (buyActivity.Quantity + stakeReward.Quantity));
					}


					holding.Activities.Remove(stakeReward!);
				}
			}

			return Task.CompletedTask;
		}
	}
}
