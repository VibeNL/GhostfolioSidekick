using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : Activity
	{
		public DividendActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, transactionId, null, null)
		{
			Amount = amount;
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		public Money Amount { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		override protected async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherDividendActivity = (DividendActivity)otherActivity;
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherDividendActivity.Amount, Amount.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherDividendActivity.Amount.Currency,
				otherDividendActivity.Date,
				Fees.Union(Taxes).ToList(),
				otherDividendActivity.Fees.Union(otherDividendActivity.Taxes).ToList());
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
