using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class StakingRewardActivity : BaseActivity<StakingRewardActivity>
	{
		public StakingRewardActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Amount = amount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public decimal Amount { get; set; }

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }
		
		public Money? CalculatedUnitPrice { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, StakingRewardActivity otherActivity)
		{
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				otherActivity.Amount);
			return Task.FromResult(quantityTimesUnitPriceEquals);
		}
	}
}
