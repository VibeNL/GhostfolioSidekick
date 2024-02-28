using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class InterestActivity : BaseActivity<InterestActivity>
	{
		public InterestActivity(
		Account account,
		DateTime dateTime,
		Money? amount,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Amount = amount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public Money? Amount { get; set; }

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, InterestActivity otherActivity)
		{
			var existingAmount = await CompareUtilities.RoundAndConvert(exchangeRateService, otherActivity.Amount, Amount?.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount?.Amount,
				existingAmount?.Amount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherActivity.Amount?.Currency,
				otherActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
