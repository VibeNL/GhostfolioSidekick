using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : BaseActivity
	{
		public DividendActivity(
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

		public IEnumerable<Money> Fees { get; set; } = [];

		public Money? Amount { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public string? Description { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		public override async Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity other)
		{
			if (other is not DividendActivity otherActivity)
			{
				return false;
			}

			if (Amount == null || otherActivity.Amount == null)
			{
				return Amount == null && otherActivity.Amount == null;
			}

			var existingUnitPrice = await RoundAndConvert(exchangeRateService, otherActivity.Amount!, Amount!.Currency, Date);
			var quantityTimesUnitPriceEquals = AreEquals(
				Amount!.Amount,
				existingUnitPrice!.Amount);
			var feesAndTaxesEquals = AreEquals(
				exchangeRateService,
				otherActivity.Amount.Currency,
				otherActivity.Date,
				Fees.Union(Taxes).ToList(),
				otherActivity.Fees.Union(otherActivity.Taxes).ToList());
			var dateEquals = Date == otherActivity.Date;
			var descriptionEquals = Description == null || Description == otherActivity.Description; // We do not create descrptions when Ghostfolio will ignore them
			var equals = quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals &&
				dateEquals &&
				descriptionEquals;
			return equals;
		}
	}
}
