using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class LiabilityActivity : Activity
	{
		internal LiabilityActivity() : base()
		{
			// EF Core
			Price = null!;
		}

		public LiabilityActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, transactionId, null, null)
		{
			Price = amount;
		}

		public Money Price { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherLiabilityActivity = (LiabilityActivity)otherActivity;
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherLiabilityActivity.Price, Price.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Price,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherLiabilityActivity.Price.Currency,
				otherLiabilityActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
