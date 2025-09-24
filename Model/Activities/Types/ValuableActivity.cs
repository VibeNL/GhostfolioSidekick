using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class ValuableActivity : ActivityWithAmount, IActivityWithPartialIdentifier
	{
		public ValuableActivity()
		{
			// EF Core
		}

		public ValuableActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, amount, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];
	}
}
