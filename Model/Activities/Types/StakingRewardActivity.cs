using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class StakingRewardActivity : ActivityWithQuantityAndUnitPrice
	{
		internal StakingRewardActivity() : base()
		{
			// EF Core
		}

		public StakingRewardActivity(
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
			var otherStakingRewardActivity = (StakingRewardActivity)otherActivity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity,
				otherStakingRewardActivity.Quantity);
			return Task.FromResult(quantityTimesUnitPriceEquals);
		}
	}
}
