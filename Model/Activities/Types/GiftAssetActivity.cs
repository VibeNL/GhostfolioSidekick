using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftAssetActivity : ActivityWithQuantityAndUnitPrice
	{
		public GiftAssetActivity()
		{			
		}

		public GiftAssetActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			decimal amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, partialSymbolIdentifiers, dateTime, amount, new Money(), transactionId, sortingPriority, description)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
