using GhostfolioSidekick.Model.Accounts;
using System.Collections.Generic;

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

       public List<Money> Fees { get; set; } = new();

	   public List<Money> Taxes { get; set; } = new();
	}
}
