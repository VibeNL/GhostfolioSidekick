using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class TaxActivity : ActivityWithAmount, IActivityWithPartialIdentifier
	{
		public TaxActivity()
		{
			// EF Core
			Amount = null!;
			PartialSymbolIdentifiers = [];
		}

		public TaxActivity(
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
