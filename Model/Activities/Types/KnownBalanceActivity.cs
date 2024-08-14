using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record KnownBalanceActivity : Activity
	{
		internal KnownBalanceActivity() : base()
		{
			// EF Core
			Amount = null!;
		}

		public KnownBalanceActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, transactionId, null, null)
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
			var otherKnownBalanceActivity = (KnownBalanceActivity)otherActivity;
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherKnownBalanceActivity.Amount, Amount.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherKnownBalanceActivity.Amount.Currency,
				otherKnownBalanceActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
