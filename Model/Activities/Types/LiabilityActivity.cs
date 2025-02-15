using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class LiabilityActivity : Activity, IActivityWithPartialIdentifier
	{
		public LiabilityActivity()
		{
			// EF Core
			Price = null!;
		}

		public LiabilityActivity(
			Account account,
			Holding? holding,	
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			Price = amount;
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public Money Price { get; set; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
