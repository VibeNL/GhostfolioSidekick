using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialActivity
	{
		public PartialActivity(
			PartialActivityType activityType,
			DateTime dateTime,
			Currency currency,
			Money TotalTransactionAmount,
			string? transactionId)
		{
			ActivityType = activityType;
			Date = dateTime.ToUniversalTime();
			Currency = currency;
			this.TotalTransactionAmount = TotalTransactionAmount;
			TransactionId = transactionId;
		}

		public PartialActivityType ActivityType { get; }

		public Currency Currency { get; }

		public DateTime Date { get; private set; }

		public decimal Amount { get; private set; }

		public string? TransactionId { get; set; }

		public PartialSymbolIdentifier[] SymbolIdentifiers { get; private set; } = [];

		public decimal? UnitPrice { get; private set; } = 1;

		public int? SortingPriority { get; private set; }

		public string? Description { get; private set; }

		public Money TotalTransactionAmount { get; private set; }

		public static PartialActivity CreateCashDeposit(
			Currency currency,
			DateTime date,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.CashDeposit, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = amount,
			};
		}

		public static PartialActivity CreateCashWithdrawal(
			Currency currency,
			DateTime date,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.CashWithdrawal, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = amount,
			};
		}

		public static PartialActivity CreateGift(
			Currency currency,
			DateTime date,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Gift, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = amount,
				Description = "Gift"
			};
		}

		public static PartialActivity CreateGift(
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Gift, date, Currency.EUR, new Money(Currency.USD, 0), transactionId)
			{
				Amount = amount,
				SymbolIdentifiers = symbolIdentifiers
			};
		}

		public static PartialActivity CreateInterest(
			Currency currency,
			DateTime date,
			decimal amount,
			string description,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Interest, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = amount,
				Description = description,
			};
		}

		public static PartialActivity CreateKnownBalance(
			Currency currency,
			DateTime date,
			decimal amount,
			int? rownumber = 0)
		{
			return new PartialActivity(PartialActivityType.KnownBalance, date, currency, new Money(Currency.USD, 0), null)
			{
				Amount = amount,
				SortingPriority = rownumber
			};
		}

		public static PartialActivity CreateTax(
			Currency currency,
			DateTime date,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Tax, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = amount,
				Description = "Tax",
			};
		}

		public static PartialActivity CreateFee(
			Currency currency,
			DateTime date,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Fee, date, currency, totalTransactionAmount, transactionId)
			{
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
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Buy, date, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
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
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Sell, date, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Amount = amount,
				UnitPrice = unitPrice
			};
		}

		public static PartialActivity CreateDividend(
			Currency currency,
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Dividend, date, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Amount = amount,
				UnitPrice = 1,
				TotalTransactionAmount = new Money(currency, amount)
			};
		}

		public static IEnumerable<PartialActivity> CreateCurrencyConvert(
			DateTime date,
			Money source,
			Money target,
			Money totalTransactionAmount,
			string transactionId)
		{
			yield return new PartialActivity(PartialActivityType.CashWithdrawal, date, source.Currency, totalTransactionAmount, transactionId)
			{
				Amount = source.Amount
			};
			yield return new PartialActivity(PartialActivityType.CashDeposit, date, target.Currency, totalTransactionAmount, transactionId)
			{
				Amount = target.Amount
			};
		}

		public static PartialActivity CreateStakingReward(
				DateTime date,
				PartialSymbolIdentifier[] symbolIdentifiers,
				decimal amount,
				string transactionId)
		{
			return new PartialActivity(PartialActivityType.StakingReward, date, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Amount = amount
			};
		}

		public static PartialActivity CreateSend(
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Send, date, Currency.USD, new Money(Currency.USD, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Amount = amount,
			};
		}

		public static PartialActivity CreateReceive(
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Receive, date, Currency.USD, new Money(Currency.USD, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Amount = amount,
			};
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "<Pending>")]
		public static IEnumerable<PartialActivity> CreateAssetConvert(
			DateTime date,
			PartialSymbolIdentifier[] source,
			decimal sourceAmount,
			decimal? sourceUnitprice,
			PartialSymbolIdentifier[] target,
			decimal targetAmount,
			decimal? targetUnitprice,
			string transactionId)
		{
			yield return new PartialActivity(PartialActivityType.Send, date, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = source,
				Amount = sourceAmount,
				UnitPrice = sourceUnitprice
			};
			yield return new PartialActivity(PartialActivityType.Receive, date, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = target,
				Amount = targetAmount,
				UnitPrice = targetUnitprice
			};
		}

		public static PartialActivity CreateValuable(
			Currency currency,
			DateTime date,
			string description,
			decimal value,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Valuable, date, currency, totalTransactionAmount, transactionId)
			{
				Amount = 1,
				UnitPrice = value,
				Description = description
			};
		}

		public static PartialActivity CreateLiability(
			Currency currency,
			DateTime date,
			string description,
			decimal value,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Liability,date, currency, totalTransactionAmount, transactionId)
			{
				Amount = 1,
				UnitPrice = value,
				Description = description
			};
		}
		
		public static PartialActivity CreateBondRepay(
			Currency currency,
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal unitPrice,
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.BondRepay, date, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				UnitPrice = unitPrice
			};
		}
	}
}