using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model.Activities.Types
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity other)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			if (other is StockSplitActivity otherActivity)
			{
				// Compare properties of this activity with the other activity
				// This is just a basic comparison, you may need to add more properties to compare based on your requirements
				return Account == otherActivity.Account &&
					   Date == otherActivity.Date &&
					   FromAmount == otherActivity.FromAmount &&
					   ToAmount == otherActivity.ToAmount &&
					   TransactionId == otherActivity.TransactionId;
			}

			return false;
		}

	}
}
