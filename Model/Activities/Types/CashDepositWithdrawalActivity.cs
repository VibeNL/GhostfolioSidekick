using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record CashDepositWithdrawalActivity : Activity
	{
		public CashDepositWithdrawalActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId) : base(account, dateTime, null, null, null)
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
			var otherCashDepositWithdrawalActivity = (CashDepositWithdrawalActivity)otherActivity;
			var existingUnitPrice = await CompareUtilities.Convert(exchangeRateService, otherCashDepositWithdrawalActivity.Amount, Amount.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Amount,
				existingUnitPrice);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherCashDepositWithdrawalActivity.Amount.Currency,
				otherCashDepositWithdrawalActivity.Date, [], []);
			return quantityTimesUnitPriceEquals && feesAndTaxesEquals;
		}
	}
}
