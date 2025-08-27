using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuyActivity : ActivityWithQuantityAndUnitPrice
	{
		public BuyActivity()
		{
			// EF Core
		}

		public BuyActivity(
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

		public virtual ICollection<BuyActivityFee> Fees { get; set; } = [];

		public virtual ICollection<BuyActivityTax> Taxes { get; set; } = [];
	}
}
