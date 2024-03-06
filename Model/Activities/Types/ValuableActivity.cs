using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class ValuableActivity : BaseActivity<ValuableActivity>
	{
		public ValuableActivity(
		Account account,
		DateTime dateTime,
		Money? amount,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Price = amount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public Money? Price { get; set; }

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, ValuableActivity otherActivity)
		{
			var existingAmount = await CompareUtilities.RoundAndConvert(exchangeRateService, otherActivity.Price, Price?.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Price?.Amount,
				existingAmount?.Amount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherActivity.Price?.Currency,
				otherActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
