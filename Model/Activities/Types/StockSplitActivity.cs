using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

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

		public string Description => "Stock split";

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"Stock split on {Date.ToInvariantDateOnlyString()} [{FromAmount}] -> [{ToAmount}]";
		}

		public Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity otherActivity)
#pragma warning restore CS1998 // Async metho
		{
			if (otherActivity is not StockSplitActivity other)
			{
				return Task.FromResult(false);
			}

			// Compare properties of this activity with the other activity
			// This is just a basic comparison, you may need to add more properties to compare based on your requirements
			return Task.FromResult(Account == other.Account &&
				   Date == other.Date &&
				   FromAmount == other.FromAmount &&
				   ToAmount == other.ToAmount &&
				   TransactionId == other.TransactionId);
		}

	}
}
