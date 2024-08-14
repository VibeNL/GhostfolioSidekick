using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuySellActivity : ActivityWithQuantityAndUnitPrice
	{
		internal BuySellActivity() : base()
		{
			// EF Core

		}

		public BuySellActivity(
		Account account,
		DateTime dateTime,
		decimal quantity,
		Money? unitPrice,
		string? transactionId,
		int? sortingPriority,
		string? description) : base(account, dateTime, quantity, unitPrice, transactionId, sortingPriority, description)
		{
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		public IEnumerable<Money> Taxes { get; set; } = [];

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherBuySellActivity = (BuySellActivity)otherActivity;
			var existingUnitPrice = await CompareUtilities.Convert(exchangeRateService, otherBuySellActivity.UnitPrice, UnitPrice?.Currency, Date);
			var quantityEquals = Quantity == otherBuySellActivity.Quantity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity * UnitPrice?.Amount,
				otherBuySellActivity.Quantity * existingUnitPrice?.Amount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherBuySellActivity.UnitPrice?.Currency,
				otherBuySellActivity.Date,
				Fees.Union(Taxes).ToList(),
				otherBuySellActivity.Fees.Union(otherBuySellActivity.Taxes).ToList());
			return quantityEquals &&
					quantityTimesUnitPriceEquals &&
					feesAndTaxesEquals;
		}
	}
}
