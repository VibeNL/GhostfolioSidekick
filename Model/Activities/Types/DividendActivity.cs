using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : ActivityWithAmount, IActivityWithPartialIdentifier, IActivityWithCosts
	{
		public DividendActivity()
		{
			// EF Core
			Amount = null!;
		}

		public DividendActivity(
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

		public List<Money> Fees { get; set; } = [];

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public List<Money> Taxes { get; set; } = [];

		public IReadOnlyList<Money> Costs => [.. Fees, .. Taxes];

		public bool IsPredicted { get; set; } = false;
	}
}
