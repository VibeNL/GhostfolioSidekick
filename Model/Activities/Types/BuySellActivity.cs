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
		ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
		DateTime dateTime,
		decimal quantity,
		Money? unitPrice,
		string transactionId,
		int? sortingPriority,
		string? description) : base(account, partialSymbolIdentifiers, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

		public ICollection<Money> Fees { get; set; } = new List<Money>();

		public ICollection<Money> Taxes { get; set; } = new List<Money>();

		public Money TotalTransactionAmount { get; set; } = new Money();

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
