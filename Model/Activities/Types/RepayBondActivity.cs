using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : Activity, IActivityWithPartialIdentifier
	{
		public RepayBondActivity()
		{
			// EF Core
			TotalRepayAmount = null!;
		}

		public RepayBondActivity(
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money totalRepayAmount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			TotalRepayAmount = totalRepayAmount;
		}

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public Money TotalRepayAmount { get; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
