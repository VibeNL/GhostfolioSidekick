using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{

	public class EnglishAccountStatementParser : BaseSubParser
	{
		private readonly string[] AccountStatementRepayment = ["DATE", "TYPE", "DESCRIPTION", "MONEY IN", "MONEY OUT", "BALANCE"];
		private readonly ColumnAlignment[] column6 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		private readonly string[] PrivateEquityTransactionsIdentification = ["Private Markets kooporder"];

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(AccountStatementRepayment, "BALANCE OVERVIEW", column6, true, new EmptyLineHeightLimitMergeStrategy(), true),
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

            var dateOnly = ParseDate(dateString ?? string.Empty);
            var date = new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);
			var moneyIn = !string.IsNullOrWhiteSpace(moneyInString) ? ParseDecimal(moneyInString) : (decimal?)null;
			var moneyOut = !string.IsNullOrWhiteSpace(moneyOutString) ? ParseDecimal(moneyOutString) : (decimal?)null;
			var balance = !string.IsNullOrWhiteSpace(balanceString) ? ParseDecimal(balanceString) : (decimal?)null;

            if (balance.HasValue)
            {
                yield return PartialActivity.CreateKnownBalance(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), balance.Value);
            }

			switch (typeString)
			{
                case "Transfer": // Transfer
                case "Card Transaction": // Card Transaction
                    {
                        if (moneyIn.HasValue)
                        {
                            yield return PartialActivity.CreateCashDeposit(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
                        }
                        else if (moneyOut.HasValue)
                        {
                            yield return PartialActivity.CreateCashWithdrawal(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyOut.Value, new Money(Currency.EUR, moneyOut.Value), transactionId);
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
                            yield return PartialActivity.CreateGift(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
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
                            yield return PartialActivity.CreateInterest(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, descriptionString, new Money(Currency.EUR, moneyIn.Value), transactionId);
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
                            yield return PartialActivity.CreateDividend(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), ParseSymbolsFromDividendStrings(descriptionString), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), transactionId);
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
                            (string symbol, decimal quantity) = ParseSymbolAndAmount(descriptionString, moneyIn.Value);
                            yield return PartialActivity.CreateSell(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateStockBondAndETF(symbol)], quantity, new Money(Currency.EUR, moneyIn.Value / quantity), new Money(Currency.EUR, moneyIn.Value), transactionId);
                        }
                        else if (moneyOut.HasValue)
                        {
                            (string symbol, decimal quantity) = ParseSymbolAndAmount(descriptionString, moneyOut.Value);
                            yield return PartialActivity.CreateBuy(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), [PartialSymbolIdentifier.CreateStockBondAndETF(symbol)], quantity, new Money(Currency.EUR, moneyOut.Value / quantity), new Money(Currency.EUR, moneyOut.Value), transactionId);
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
		///  Sell trade US2546871060 DISNEY (WALT) CO., quantity: 1.640151
		/// </summary>
		/// <param name="descriptionString">The full transaction description containing the ISIN and quantity.</param>
		/// <param name="amount">The total monetary amount of the transaction.</param>
		/// <returns>A tuple containing the parsed symbol (ISIN or placeholder) and quantity.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="descriptionString"/> is <see langword="null"/> or empty.</exception>
		/// <exception cref="InvalidOperationException">The quantity part cannot be found in <paramref name="descriptionString"/>.</exception>
		/// <exception cref="FormatException">The quantity part cannot be parsed as a decimal number.</exception>
		private (string symbol, decimal amount) ParseSymbolAndAmount(string descriptionString, decimal amount)
		{
			// Get the quantity from the string
			if (string.IsNullOrEmpty(descriptionString))
			{
				throw new ArgumentNullException(nameof(descriptionString));
			}

			if (PrivateEquityTransactionsIdentification.Contains(descriptionString))
			{
				return ("PRIVATE_EQUITY", amount);
			}

			var quantityPrefix = "quantity: ";
			var quantityIndex = descriptionString.IndexOf(quantityPrefix);
			if (quantityIndex < 0)
			{
				throw new InvalidOperationException(
					$"Unable to find quantity in description: {descriptionString}");
			}

			var quantityString = descriptionString.Substring(quantityIndex + quantityPrefix.Length).Trim();
			if (!decimal.TryParse(quantityString, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
			{
				throw new FormatException($"Unable to parse quantity: {quantityString}");
			}

			// Lets us the ISINParser to search for the isin
			foreach (var text in descriptionString.Split([' '], StringSplitOptions.RemoveEmptyEntries))
			{
				var isin = ISINParser.ExtractIsin(text);
				if (!string.IsNullOrWhiteSpace(isin))
				{
					return (isin, quantity); // Placeholder for amount, adjust as needed
				}
			}

			return (string.Empty, 0); // Return a default value if no ISIN is found
		}

		/// <summary>
		/// Get the symbol from strings like:
		///  Cash Dividend for ISIN IE00BM8R0J59
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static ICollection<PartialSymbolIdentifier> ParseSymbolsFromDividendStrings(string descriptionString)
		{
			var isinPrefix = "ISIN ";
			var isinIndex = descriptionString.IndexOf(isinPrefix);
			if (isinIndex >= 0)
			{
				var isin = descriptionString[(isinIndex + isinPrefix.Length)..].Trim();
				return [PartialSymbolIdentifier.CreateStockAndETF(isin)];
			}
			return [];
		}

		private static DateOnly ParseDate(string dateString)
		{
			if (DateOnly.TryParseExact(dateString, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				return date;
			}

			throw new FormatException($"Unable to parse date: {dateString}");
		}
	}
}
