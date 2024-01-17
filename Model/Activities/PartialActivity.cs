using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class PartialActivity(SymbolProfile symbolProfile)
	{
		public SymbolProfile SymbolProfile { get; set; } = symbolProfile;

		public static PartialActivity CreateCashDeposit(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateCashWithdrawal(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateGift(Currency currencyTotal, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateInterest(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateKnownBalance(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateTax(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateFee(Currency currency, DateTime date, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateBuy(
			Currency currency,
			DateTime date,
			string symbolIdentifier,
			decimal quantity,
			decimal unitPrice,
			string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateSell(
			Currency currency,
			DateTime date,
			string symbolIdentifier,
			decimal quantity,
			decimal unitPrice,
			string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateDividend(Currency currency, DateTime date, string symbolIdentifier, decimal amount, string? transactionId)
		{
			throw new NotImplementedException();
		}

		public static PartialActivity CreateCurrencyConvert(DateTime date, Money source, Money target, string? id)
		{
			throw new NotImplementedException();
		}
	}
}