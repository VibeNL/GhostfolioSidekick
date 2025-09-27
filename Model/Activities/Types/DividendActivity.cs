using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : ActivityWithAmount, IActivityWithPartialIdentifier
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

		public virtual ICollection<DividendActivityFee> Fees { get; set; } = [];

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public virtual ICollection<DividendActivityTax> Taxes { get; set; } = [];
	}
}
