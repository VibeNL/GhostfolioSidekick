using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	[method: SetsRequiredMembers]
	public class PartialActivity(ActivityType activityType, Currency currency, string? transactionId)
	{
		public ActivityType ActivityType { get; } = activityType;
		public Currency Currency { get; } = currency;
		public DateTime Date { get; private set; }
		public decimal Amount { get; private set; }
		public string? TransactionId { get; } = transactionId;
		public PartialSymbolIdentifier[] SymbolIdentifiers { get; private set; } = [];
		public decimal? UnitPrice { get; private set; }

		public static PartialActivity CreateCashDeposit(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.CashDeposit, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateCashWithdrawal(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.CashWithdrawal, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateGift(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Gift, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateInterest(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Interest, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateKnownBalance(Currency currency, DateTime date, decimal amount)
		{
			return new PartialActivity(ActivityType.KnownBalance, currency, null)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateTax(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Tax, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateFee(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Fee, currency, transactionId)
			{
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateBuy(
			Currency currency,
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			decimal unitPrice,
			string transactionId)
		{
			return new PartialActivity(ActivityType.Buy, currency, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
				UnitPrice = unitPrice
			};
		}

		public static PartialActivity CreateSell(
			Currency currency,
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			decimal unitPrice,
			string transactionId)
		{
			return new PartialActivity(ActivityType.Sell, currency, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
				UnitPrice = unitPrice
			};
		}

		public static PartialActivity CreateDividend(
		Currency currency, 
		DateTime date,
		PartialSymbolIdentifier[] symbolIdentifiers,
		decimal amount, 
		string transactionId)
		{
			return new PartialActivity(ActivityType.Dividend, currency, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
				UnitPrice = 1
			};
		}

		public static IEnumerable<PartialActivity> CreateCurrencyConvert(
			DateTime date, 
			Money source, 
			Money target,
			string transactionId)
		{
			yield return new PartialActivity(ActivityType.CashWithdrawal, source.Currency, transactionId)
			{
				Date = date,
				Amount = source.Amount
			};
			yield return new PartialActivity(ActivityType.CashDeposit, target.Currency, transactionId)
			{
				Date = date,
				Amount = target.Amount
			};
		}
	}
}