using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class SellActivity : ActivityWithQuantityAndUnitPrice, IActivityWithCosts
	{
		public SellActivity()
		{
			// EF Core
		}

		public SellActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal quantity,
			Money unitPrice,
			Money transactionAmount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, quantity, unitPrice, transactionAmount, transactionId, sortingPriority, description)
		{
		}

		public List<Money> Fees { get; set; } = [];

		public List<Money> Taxes { get; set; } = [];

		public IReadOnlyList<Money> Costs => [.. Fees, .. Taxes];
	}
}
