using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : ActivityWithAmount, IActivityWithPartialIdentifier
	{
		public RepayBondActivity()
		{
			// EF Core
		}

		public RepayBondActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money totalRepayAmount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, totalRepayAmount, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];
	}
}
