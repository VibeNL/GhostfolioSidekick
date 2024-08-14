using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record StockSplitActivity : Activity
	{
		internal StockSplitActivity() : base()
		{
			// EF Core
		}

		public StockSplitActivity(
			Account account,
			DateTime dateTime,
			int fromAmount,
			int toAmount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, "Stock split")
		{
			FromAmount = fromAmount;
			ToAmount = toAmount;
		}

		public int FromAmount { get; }

		public int ToAmount { get; }
		
		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"Stock split on {Date.ToInvariantDateOnlyString()} [{FromAmount}] -> [{ToAmount}]";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherStockSplitActivity = (StockSplitActivity)otherActivity;
			// Compare properties of this activity with the other activity
			// This is just a basic comparison, you may need to add more properties to compare based on your requirements
			return Task.FromResult(Account == otherStockSplitActivity.Account &&
				   Date == otherStockSplitActivity.Date &&
				   FromAmount == otherStockSplitActivity.FromAmount &&
				   ToAmount == otherStockSplitActivity.ToAmount &&
				   TransactionId == otherStockSplitActivity.TransactionId);
		}
	}
}
