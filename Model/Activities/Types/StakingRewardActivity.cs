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
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, partialSymbolIdentifiers, dateTime, amount, null, transactionId, sortingPriority, description)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
