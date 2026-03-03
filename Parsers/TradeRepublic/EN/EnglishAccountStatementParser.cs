using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using iText.Kernel.XMP.Impl;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{

	public class EnglishAccountStatementParser : BaseSubParser
	{
		private readonly string[] AccountStatementRepayment = ["DATE", "TYPE", "DESCRIPTION", "MONEY IN", "MONEY OUT", "BALANCE"];
		private readonly ColumnAlignment[] column6 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(AccountStatementRepayment, "BALANCE OVERVIEW", column6, true, new EmptyLineHeightLimitMergeStrategy()),
				];
			}
		}

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var dateString = row.GetColumnValue("DATE") ?? string.Empty;
			var typeString = row.GetColumnValue("TYPE") ?? string.Empty;
			var descriptionString = row.GetColumnValue("DESCRIPTION") ?? string.Empty;
			var moneyInString = row.GetColumnValue("MONEY IN") ?? string.Empty;
			var moneyOutString = row.GetColumnValue("MONEY OUT") ?? string.Empty;
			var balanceString = row.GetColumnValue("BALANCE") ?? string.Empty;

			var date = ParseDate(dateString ?? string.Empty).ToDateTime(TimeOnly.MinValue);
			var moneyIn = !string.IsNullOrWhiteSpace(moneyInString) ? ParseDecimal(moneyInString) : (decimal?)null;
			var moneyOut = !string.IsNullOrWhiteSpace(moneyOutString) ? ParseDecimal(moneyOutString) : (decimal?)null;
			var balance = !string.IsNullOrWhiteSpace(balanceString) ? ParseDecimal(balanceString) : (decimal?)null;

			if (balance.HasValue)
			{
				yield return PartialActivity.CreateKnownBalance(Currency.EUR, date, balance.Value);
			}

			switch (typeString)
			{
				case "Transfer": // Transfer
				case "Card Transaction": // Card Transaction
					{
						if (moneyIn.HasValue)
						{
							yield return PartialActivity.CreateCashDeposit(Currency.EUR, date, moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
						}
						else if (moneyOut.HasValue)
						{
							yield return PartialActivity.CreateCashWithdrawal(Currency.EUR, date, moneyOut.Value, new Money(Currency.EUR, moneyOut.Value), transactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}

						break;
					}
				case "Reward": // Cashback
					{
						if (moneyIn.HasValue)
						{
							yield return PartialActivity.CreateGift(Currency.EUR, date, moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
					}
				case "Interest": // Interest
					{
						if (moneyIn.HasValue)
						{
							yield return PartialActivity.CreateInterest(Currency.EUR, date, moneyIn.Value, descriptionString, new Money(Currency.EUR, moneyIn.Value), transactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
					}
				case "Earnings": // Dividend
					{
						if (moneyIn.HasValue)
						{
							yield return PartialActivity.CreateDividend(Currency.EUR, date, ParseSymbolsFromDividendStrings(descriptionString), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
					}
				case "Trade": // Buys and Sells
					{
						if (moneyIn.HasValue)
						{
							(string symbol, decimal amount) = ParseSymbolAndAmount(descriptionString);
							yield return PartialActivity.CreateSell(Currency.EUR, date, [PartialSymbolIdentifier.CreateStockAndETF(symbol)], amount, new Money(Currency.EUR, moneyIn.Value / amount),  new Money(Currency.EUR, moneyIn.Value), transactionId);
						}
						else if (moneyOut.HasValue)
						{
							(string symbol, decimal amount) = ParseSymbolAndAmount(descriptionString);
							yield return PartialActivity.CreateBuy(Currency.EUR, date, [PartialSymbolIdentifier.CreateStockAndETF(symbol)], amount, new Money(Currency.EUR, moneyOut.Value / amount), new Money(Currency.EUR, moneyOut.Value), transactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
					}
			}
		}

		/// <summary>
		/// Parse the information from strings like:
		///  Savings plan execution IE00BM8R0J59 Global X ETFs ICAV - Global X Nasdaq 100 Covered Call UCITS ETF Dis USD, quantity: 1.744591
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private (string symbol, decimal amount) ParseSymbolAndAmount(string descriptionString)
		{
			var quantityPrefix = "quantity: ";
			var quantityIndex = descriptionString.IndexOf(quantityPrefix);
			if (quantityIndex >= 0)
			{
				var quantityString = descriptionString.Substring(quantityIndex + quantityPrefix.Length).Trim();
				if (decimal.TryParse(quantityString, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
				{
					var isinPrefix = "IE";
					var isinIndex = descriptionString.IndexOf(isinPrefix);
					if (isinIndex >= 0)
					{
						var isin = descriptionString.Substring(isinIndex, 12).Trim();
						return (isin, quantity);
					}
				}
			}
			
			throw new FormatException($"Unable to parse symbol and amount from description: {descriptionString}");
		}

		/// <summary>
		/// Get the symbol from strings like:
		///  Cash Dividend for ISIN IE00BM8R0J59
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private ICollection<PartialSymbolIdentifier> ParseSymbolsFromDividendStrings(string descriptionString)
		{
			var isinPrefix = "ISIN ";
			var isinIndex = descriptionString.IndexOf(isinPrefix);
			if (isinIndex >= 0)
			{
				var isin = descriptionString.Substring(isinIndex + isinPrefix.Length).Trim();
				return [PartialSymbolIdentifier.CreateStockAndETF(isin)];
			}
			return [];
		}

		private DateOnly ParseDate(string dateString)
		{
			if (DateOnly.TryParseExact(dateString, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				return date;
			}

			throw new FormatException($"Unable to parse date: {dateString}");
		}
	}
}
