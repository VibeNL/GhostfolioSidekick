using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : BaseActivity<DividendActivity>
	{
		public DividendActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Amount = amount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public IEnumerable<Money> Fees { get; set; } = [];

		public Money Amount { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		override protected async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, DividendActivity otherActivity)
		{
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherActivity.Amount, Amount.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherActivity.Amount.Currency,
				otherActivity.Date,
				Fees.Union(Taxes).ToList(),
				otherActivity.Fees.Union(otherActivity.Taxes).ToList());
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
