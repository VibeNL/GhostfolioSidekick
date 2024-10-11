using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class ValuableActivity : Activity, IActivityWithPartialIdentifier
	{
		public ValuableActivity()
		{
			// EF Core
			Price = null!;
		}

		public ValuableActivity(
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			Price = amount;
		}

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public Money Price { get; set; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
