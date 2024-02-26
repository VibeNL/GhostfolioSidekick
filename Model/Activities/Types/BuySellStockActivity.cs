using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class BuySellActivity : BaseActivity
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

		public override int? SortingPriority { get; }

		public string? Description { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		public override async Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity other)
		{
			if (other is not BuySellActivity otherActivity)
			{
				return false;
			}

			if (UnitPrice == null || otherActivity.UnitPrice == null)
			{
				return UnitPrice == null && otherActivity.UnitPrice == null;
			}

			var existingUnitPrice = await RoundAndConvert(exchangeRateService, otherActivity.UnitPrice!, UnitPrice!.Currency, Date);
			var quantityTimesUnitPriceEquals = AreEquals(
				Quantity * UnitPrice!.Amount,
				otherActivity.Quantity * existingUnitPrice!.Amount);
			var feesAndTaxesEquals = AreEquals(
				exchangeRateService,
				otherActivity.UnitPrice.Currency,
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
