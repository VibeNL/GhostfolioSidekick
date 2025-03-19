using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuySellActivity : ActivityWithQuantityAndUnitPrice
	{
		public BuySellActivity()
		{
			// EF Core
		}

		public BuySellActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal quantity,
			Money unitPrice,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

		public virtual ICollection<BuySellActivityFee> Fees { get; set; } = [];

		public virtual ICollection<BuySellActivityTax> Taxes { get; set; } = [];

		public Money TotalTransactionAmount { get; set; } = new Money();
	}
}
