using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuySellActivity : BaseActivity<BuySellActivity>, IActivityWithQuantityAndUnitPrice
	{
		public BuySellActivity(
		Account account,
		DateTime dateTime,
		decimal quantity,
		Money? unitPrice,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Quantity = quantity;
			UnitPrice = unitPrice;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }


		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, BuySellActivity otherActivity)
		{
			var existingUnitPrice = await CompareUtilities.Convert(exchangeRateService, otherActivity.UnitPrice, UnitPrice?.Currency, Date);
			var quantityEquals = Quantity == otherActivity.Quantity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity * UnitPrice?.Amount,
				otherActivity.Quantity * existingUnitPrice?.Amount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherActivity.UnitPrice?.Currency,
				otherActivity.Date,
				Fees.Union(Taxes).ToList(),
				otherActivity.Fees.Union(otherActivity.Taxes).ToList());
			return quantityEquals &&
					quantityTimesUnitPriceEquals &&
					feesAndTaxesEquals;
		}
	}
}
