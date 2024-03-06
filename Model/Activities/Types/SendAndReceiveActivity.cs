using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record SendAndReceiveActivity : BaseActivity<SendAndReceiveActivity>, IActivityWithQuantityAndUnitPrice
	{
		public SendAndReceiveActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			Quantity = amount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public decimal Quantity { get; set; }

		public Money? UnitPrice { get; set; }

		public IEnumerable<Money> Fees { get; set; } = [];

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, SendAndReceiveActivity otherActivity)
		{
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity,
				otherActivity.Quantity);

			var feesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherActivity.UnitPrice?.Currency ?? Currency.USD,
				otherActivity.Date,
				Fees.ToList(),
				otherActivity.Fees.ToList());
			return Task.FromResult(quantityTimesUnitPriceEquals && feesEquals);
		}
	}
}
