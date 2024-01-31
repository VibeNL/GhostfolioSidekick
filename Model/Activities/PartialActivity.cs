using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	[method: SetsRequiredMembers]
	public class PartialActivity(ActivityType activityType, Currency currency, string? transactionId)
	{
		private DateTime date;

		public ActivityType ActivityType { get; } = activityType;
		public Currency Currency { get; } = currency;
		public DateTime Date { get => date; private set => date = value.ToUniversalTime(); }
		public decimal Amount { get; private set; }
		public string? TransactionId { get; } = transactionId;
		public PartialSymbolIdentifier[] SymbolIdentifiers { get; private set; } = [];
		public decimal? UnitPrice { get; private set; } = 1;
		public int? SortingPriority { get; private set; }
		public string? Description { get; private set; }

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
				Amount = amount,
				Description = "Gift",
			};
		}

		public static PartialActivity CreateGift(DateTime date, PartialSymbolIdentifier[] symbolIdentifiers, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Gift, Currency.EUR, transactionId)
			{
				Date = date,
				Amount = amount,
				SymbolIdentifiers = symbolIdentifiers
			};
		}

		public static PartialActivity CreateInterest(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Interest, currency, transactionId)
			{
				Date = date,
				Amount = amount,
				Description = "Interest",
			};
		}

		public static PartialActivity CreateKnownBalance(Currency currency, DateTime date, decimal amount, int? rownumber = 0)
		{
			return new PartialActivity(ActivityType.KnownBalance, currency, null)
			{
				Date = date,
				Amount = amount,
				SortingPriority = rownumber
			};
		}

		public static PartialActivity CreateTax(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Tax, currency, transactionId)
			{
				Date = date,
				Amount = amount,
				Description = "Tax",
			};
		}

		public static PartialActivity CreateFee(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Fee, currency, transactionId)
			{
				Date = date,
				Amount = amount,
				Description = "Fee",
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

		public static PartialActivity CreateStakingReward(DateTime date, PartialSymbolIdentifier[] symbolIdentifiers, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.StakingReward, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateSend(DateTime date, PartialSymbolIdentifier[] symbolIdentifiers, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Send, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
			};
		}

		public static PartialActivity CreateRecieve(DateTime date, PartialSymbolIdentifier[] symbolIdentifiers, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.Receive, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
			};
		}

		public static PartialActivity CreateLearningReward(DateTime date, PartialSymbolIdentifier[] symbolIdentifiers, decimal amount, string transactionId)
		{
			return new PartialActivity(ActivityType.LearningReward, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount
			};
		}

		public static IEnumerable<PartialActivity> CreateAssetConvert(
			DateTime date,
			PartialSymbolIdentifier[] source,
			decimal sourceAmount,
			PartialSymbolIdentifier[] target,
			decimal targetAmount,
			string transactionId)

		{
			yield return new PartialActivity(ActivityType.Send, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = source,
				Date = date,
				Amount = sourceAmount,
			};
			yield return new PartialActivity(ActivityType.Receive, Currency.EUR, transactionId)
			{
				SymbolIdentifiers = target,
				Date = date,
				Amount = targetAmount,
			};
		}

		public static PartialActivity CreateValuable(Currency currency, DateTime date, string description, decimal value, string transactionId)
		{
			return new PartialActivity(ActivityType.Valuable, currency, transactionId)
			{
				Date = date,
				Amount = 1,
				UnitPrice = value,
				Description = description
			};
		}
	}
}