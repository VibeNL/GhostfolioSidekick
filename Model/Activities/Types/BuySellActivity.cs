using GhostfolioSidekick.Model.Accounts;

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
			Money? unitPrice,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

		public ICollection<Money> Fees { get; set; } = [];

		public ICollection<Money> Taxes { get; set; } = [];

		public Money TotalTransactionAmount { get; set; } = new Money();

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
