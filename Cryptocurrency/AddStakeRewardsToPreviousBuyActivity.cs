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

			var stakeRewards = activities
				.Select(x => x as StakingRewardActivity)
				.Where(x => x != null)
				.ToList();

			foreach (var stakeReward in stakeRewards)
			{
				var buyActivity = activities
					.Select(x => x as IActivityWithQuantityAndUnitPrice)
					.Where(x => x != null)
					.LastOrDefault(x => x!.Quantity > 0 &&
										x!.Date < stakeReward!.Date);

				if (buyActivity != null)
				{
					buyActivity.Quantity += stakeReward!.Quantity;
					holding.Activities.Remove(stakeReward!);
				}
			}

			return Task.CompletedTask;
		}
	}
}
