using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class FeeActivity : Activity
	{
		internal FeeActivity() : base()
		{
			// EF Core
			Amount = null!;
		}

		public FeeActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public Money Amount { get; set; }
		
		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherFeeActivity = (FeeActivity)otherActivity;
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherFeeActivity.Amount, Amount.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherFeeActivity.Amount.Currency,
				otherFeeActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
