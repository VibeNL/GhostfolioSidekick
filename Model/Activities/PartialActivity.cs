namespace GhostfolioSidekick.Model.Activities
{
	public class PartialActivity
	{
		private DateTime date;
		public PartialActivity(
			PartialActivityType activityType,
			Currency currency,
			Money TotalTransactionAmount,
			string? transactionId)
		{
			ActivityType = activityType;
			Currency = currency;
			this.TotalTransactionAmount = TotalTransactionAmount;
			TransactionId = transactionId;
		}

		public PartialActivityType ActivityType { get; }

		public Currency Currency { get; }

		public DateTime Date { get => date; private set => date = value.ToUniversalTime(); }

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
			return new PartialActivity(PartialActivityType.CashDeposit, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.CashWithdrawal, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Gift, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Gift, Currency.EUR, new Money(Currency.USD, 0), transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Interest, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.KnownBalance, currency, new Money(Currency.USD, 0), null)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Tax, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Fee, currency, totalTransactionAmount, transactionId)
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
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Buy, currency, totalTransactionAmount, transactionId)
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
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Sell, currency, totalTransactionAmount, transactionId)
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
			Money totalTransactionAmount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Dividend, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
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
			yield return new PartialActivity(PartialActivityType.CashWithdrawal, source.Currency, totalTransactionAmount, transactionId)
			{
				Date = date,
				Amount = source.Amount
			};
			yield return new PartialActivity(PartialActivityType.CashDeposit, target.Currency, totalTransactionAmount, transactionId)
			{
				Date = date,
				Amount = target.Amount
			};
		}

		public static PartialActivity CreateStakingReward(
				DateTime date,
				PartialSymbolIdentifier[] symbolIdentifiers,
				decimal amount,
				string transactionId)
		{
			return new PartialActivity(PartialActivityType.StakingReward, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount
			};
		}

		public static PartialActivity CreateSend(
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Send, Currency.USD, new Money(Currency.USD, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				Amount = amount,
			};
		}

		public static PartialActivity CreateReceive(
			DateTime date,
			PartialSymbolIdentifier[] symbolIdentifiers,
			decimal amount,
			string transactionId)
		{
			return new PartialActivity(PartialActivityType.Receive, Currency.USD, new Money(Currency.USD, 0), transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
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
			yield return new PartialActivity(PartialActivityType.Send, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = source,
				Date = date,
				Amount = sourceAmount,
				UnitPrice = sourceUnitprice
			};
			yield return new PartialActivity(PartialActivityType.Receive, Currency.USD, new Money(Currency.EUR, 0), transactionId)
			{
				SymbolIdentifiers = target,
				Date = date,
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
			return new PartialActivity(PartialActivityType.Valuable, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.Liability, currency, totalTransactionAmount, transactionId)
			{
				Date = date,
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
			return new PartialActivity(PartialActivityType.BondRepay, currency, totalTransactionAmount, transactionId)
			{
				SymbolIdentifiers = symbolIdentifiers,
				Date = date,
				UnitPrice = unitPrice
			};
		}
	}
}