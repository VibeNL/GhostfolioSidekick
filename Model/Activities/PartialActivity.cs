using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	[method: SetsRequiredMembers]
	public class PartialActivity(ActivityType activityType, Currency currency)
	{
		public ActivityType ActivityType { get; } = activityType;
		public Currency Currency { get; } = currency;
		public DateTime Date { get; private set; }
		public decimal Amount { get; private set; }
		public string? TransactionId { get; private set; }
		public string? SymbolIdentifier { get; private set; }
		public decimal? UnitPrice { get; private set; }

		public static PartialActivity CreateCashDeposit(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.CashDeposit, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateCashWithdrawal(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.CashWithdrawal, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateGift(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.Gift, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateInterest(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.Interest, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateKnownBalance(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.KnownBalance, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateTax(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.Tax, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateFee(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.Fee, currency)
			{
				Date = date,
				Amount = amount,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateBuy(
			Currency currency,
			DateTime date,
			string symbolIdentifier,
			decimal amount,
			decimal unitPrice,
			string? transactionId)
		{
			return new PartialActivity(ActivityType.Buy, currency)
			{
				SymbolIdentifier = symbolIdentifier,
				Date = date,
				Amount = amount,
				UnitPrice = unitPrice,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateSell(
			Currency currency,
			DateTime date,
			string symbolIdentifier,
			decimal amount,
			decimal unitPrice,
			string? transactionId)
		{
			return new PartialActivity(ActivityType.Sell, currency)
			{
				SymbolIdentifier = symbolIdentifier,
				Date = date,
				Amount = amount,
				UnitPrice = unitPrice,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateDividend(Currency currency, DateTime date, string symbolIdentifier, decimal amount, string? transactionId)
		{
			return new PartialActivity(ActivityType.Dividend, currency)
			{
				SymbolIdentifier = symbolIdentifier,
				Date = date,
				Amount = amount,
				UnitPrice = 1,
				TransactionId = transactionId
			};
		}

		public static PartialActivity CreateCurrencyConvert(DateTime date, Money source, Money target, string? transactionId)
		{
			throw new NotSupportedException();
		}
	}
}