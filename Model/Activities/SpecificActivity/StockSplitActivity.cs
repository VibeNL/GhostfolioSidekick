using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.SpecificActivity
{
	public record StockSplitActivity : IActivity
	{
		public StockSplitActivity(
			Account account,
			DateTime dateTime,
			int fromAmount,
			int toAmount,
			string? transactionId)
		{
			Account = account;
			Date = dateTime;
			FromAmount = fromAmount;
			ToAmount = toAmount;
			TransactionId = transactionId;
		}

		public Account Account { get; }

		public DateTime Date { get; }

		public int FromAmount { get; }

		public int ToAmount { get; }

		public string? TransactionId { get; }

		public int? SortingPriority { get; set; }

		public string? Id { get; set; }
	}
}
