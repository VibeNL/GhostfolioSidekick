using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class StakingRewardActivity : ActivityWithQuantityAndUnitPrice
	{
		public StakingRewardActivity()
		{
			// EF Core
		}

		public StakingRewardActivity(
			SymbolProfile? symbolProfile,
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(symbolProfile, account, partialSymbolIdentifiers, dateTime, amount, null, transactionId, sortingPriority, description)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
