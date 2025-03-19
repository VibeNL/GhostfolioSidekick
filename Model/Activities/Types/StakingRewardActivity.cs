using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class StakingRewardActivity : ActivityWithQuantityAndUnitPrice
	{
		public StakingRewardActivity()
		{
			// EF Core
		}

		public StakingRewardActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, amount, new Money(), transactionId, sortingPriority, description)
		{
		}
	}
}
