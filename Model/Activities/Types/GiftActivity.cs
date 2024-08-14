using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftActivity : ActivityWithQuantityAndUnitPrice
	{
		internal GiftActivity() : base()
		{
			// EF Core
		}

		public GiftActivity(
			Account account,
			DateTime dateTime,
			decimal amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, amount, null, transactionId, sortingPriority, description)
		{
		}

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherGiftActivity = (GiftActivity)otherActivity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity,
				otherGiftActivity.Quantity);
			return Task.FromResult(quantityTimesUnitPriceEquals);
		}
	}
}
