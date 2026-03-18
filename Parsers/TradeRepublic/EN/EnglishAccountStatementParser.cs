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

			var newTransactionId = $"{transactionId ?? string.Empty}_{dateOnly:yyyyMMdd}_{GetHash(dateString, typeString, descriptionString, moneyInString, moneyOutString, balanceString)}";

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
							yield return PartialActivity.CreateCashDeposit(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), newTransactionId);
						}
						else if (moneyOut.HasValue)
						{
							yield return PartialActivity.CreateCashWithdrawal(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyOut.Value, new Money(Currency.EUR, moneyOut.Value), newTransactionId);
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
							yield return PartialActivity.CreateGift(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, new Money(Currency.EUR, moneyIn.Value), newTransactionId);
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
							yield return PartialActivity.CreateInterest(Currency.EUR, DateTime.SpecifyKind(date, DateTimeKind.Utc), moneyIn.Value, descriptionString, new Money(Currency.EUR, moneyIn.Value), newTransactionId);
						}
						else
						{
							throw new InvalidOperationException();
						}
						break;
					}
				case "Earnings": // Dividend
				case "Trade": // Buys and Sells
					{
						// Ignore, should be separate statement files
						break;
					}
			}
		}

		private static string GetHash(string? dateString, string typeString, string descriptionString, string moneyInString, string moneyOutString, string balanceString)
		{
			// Generate SHA256 hash of the combined string
			var combinedString = $"{dateString}|{typeString}|{descriptionString}|{moneyInString}|{moneyOutString}|{balanceString}";
			var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combinedString));
			return Convert.ToBase64String(hashBytes);
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
