using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class StakingRewardActivity : ActivityWithQuantityAndUnitPrice
	{
		internal StakingRewardActivity()
		{
		}

		public StakingRewardActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId) : base(account, dateTime, amount, null, transactionId, null, null)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
