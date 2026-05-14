using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class SellActivity : ActivityWithQuantityAndUnitPrice
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
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

       public List<Money> Fees { get; set; } = new();

	   public List<Money> Taxes { get; set; } = new();
	}
}
