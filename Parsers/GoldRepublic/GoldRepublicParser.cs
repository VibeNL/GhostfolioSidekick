using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System;
using System.Globalization;
using System.Transactions;

namespace GhostfolioSidekick.Parsers.GoldRepublic
{
	public partial class GoldRepublicParser(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		private static readonly string[] HeaderKeywords = ["Transaction Type", "Date", "Description", "Bullion", "Amount", "Balance"];

		protected override bool IgnoreFooter => true;

		protected override int FooterHeightThreshold => 50;

		private class DescriptionData
		{
			public DateTime ExecutionDate { get; set; }
			public string Action { get; set; } = string.Empty;
			public decimal TransactionValue { get; set; }
			public decimal Fee { get; set; }
			public decimal Volume { get; set; }
			public decimal Total { get; set; }
		}

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			try
			{
				bool hasGoldRepublic = ContainsSequence(["WWW.GOLDREPUBLIC.COM"], words);
				bool hasAccountStatement = ContainsSequence(["Account", "Statement"], words);

				return hasGoldRepublic && hasAccountStatement;
			}
			catch
			{
				return false;
			}
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			bool StopPredicate(PdfTableRow row) => row.Text.Contains("Closing balance", StringComparison.InvariantCultureIgnoreCase);

			bool MergePredicate(PdfTableRow current, PdfTableRow next)
			{
				// Merge if the first column of the next row is empty (indicating continuation of previous row)
				if (next.Tokens.Count == 0)
				{
					return false;
				}

				// Check if the first token of the next row has the same or very similar horizontal position as subsequent columns
				// This indicates that the first column is empty and this is a continuation row
				var firstToken = next.Tokens.FirstOrDefault();
				if (firstToken?.BoundingBox == null)
				{
					return false;
				}

				// Get the first token's column position
				var firstTokenColumn = firstToken.BoundingBox.Column;

				// If current row has tokens, check if the first token of next row aligns with non-first columns
				if (current.Tokens.Count > 1)
				{
					// Find the second column position from current row to determine if next row starts there
					var currentSecondColumnPosition = current.Tokens
						.Where(t => t.BoundingBox != null)
						.Skip(1)
						.FirstOrDefault()?.BoundingBox?.Column;

					if (currentSecondColumnPosition.HasValue)
					{
						// If the first token of next row is closer to the second column position,
						// it likely means the first column is empty
						var distanceToSecond = Math.Abs(firstTokenColumn - currentSecondColumnPosition.Value);
						var distanceToFirst = current.Tokens.FirstOrDefault()?.BoundingBox?.Column is int firstCol
							? Math.Abs(firstTokenColumn - firstCol)
							: int.MaxValue;

						return distanceToSecond < distanceToFirst;
					}
				}

				return false;
			}

			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				HeaderKeywords,
				stopPredicate: StopPredicate,
				mergePredicate: MergePredicate);

			foreach (var row in rows)
			{
				var parsed = ParseRecord(header, row);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		private IEnumerable<PartialActivity>? ParseRecord(PdfTableRow header, PdfTableRowColumns row)
		{
			// Get values
			// Transaction Type
			var transactionType = row.GetColumnValue(header, "Transaction Type")?.Trim() ?? string.Empty;
			var date = row.GetColumnValue(header, "Date")?.Trim();
			var bullion = row.GetColumnValue(header, "Bullion")?.Trim();
			var amount = row.GetColumnValue(header, "Amount")?.Trim();
			var balance = row.GetColumnValue(header, "Balance")?.Trim();

			var dateParsed = ParseDate(date);
			var amountParsed = ParseDecimal(amount);
			var balanceParsed = ParseDecimal(balance);
			var transactionId = row.Text;

			if (string.IsNullOrEmpty(transactionType) || string.IsNullOrEmpty(date))
			{
				yield break;
			}

			if (transactionType.Equals("Deposit", StringComparison.InvariantCultureIgnoreCase) ||
				transactionType.Equals("Direct Debit", StringComparison.InvariantCultureIgnoreCase))
			{
				yield return PartialActivity.CreateCashDeposit(
					Currency.EUR,
					dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
					amountParsed,
					new Money(Currency.EUR, amountParsed),
					transactionId
					);
			}

			if (transactionType.Equals("Withdrawal", StringComparison.InvariantCultureIgnoreCase))
			{
				yield return PartialActivity.CreateCashWithdrawal(
					Currency.EUR,
					dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
					amountParsed,
					new Money(Currency.EUR, amountParsed),
					transactionId
					);
			}

			if (transactionType.Equals("Cost Order", StringComparison.InvariantCultureIgnoreCase))
			{
				yield return PartialActivity.CreateFee(
					Currency.EUR,
					dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
					amountParsed * -1,
					new Money(Currency.EUR, amountParsed * -1),
					transactionId
					);
			}

			if (transactionType.Equals("Market Order", StringComparison.InvariantCultureIgnoreCase) ||
				transactionType.Equals("Savings Order", StringComparison.InvariantCultureIgnoreCase))
			{
				var subTable = ParseDescription(row.Columns[2]); // Description is usually the 3rd column
				var executionDate = subTable.ExecutionDate;
				var action = subTable.Action;
				var transactionValue = subTable.TransactionValue;
				var fee = subTable.Fee;
				var volume = subTable.Volume / 1000; // In gram, store as KG
				var total = subTable.Total;

				if (action.Equals("Sell", StringComparison.InvariantCultureIgnoreCase))
				{
					yield return PartialActivity.CreateSell(
						Currency.EUR,
						dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric(bullion ?? "<??>")],
						volume,
						new Money(Currency.EUR, transactionValue / volume),
						new Money(Currency.EUR, transactionValue),
						transactionId
						);
				}
				else
				{
					yield return PartialActivity.CreateBuy(
						Currency.EUR,
						dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric(bullion ?? "<??>")],
						volume,
						new Money(Currency.EUR, transactionValue / volume),
						new Money(Currency.EUR, transactionValue),
						transactionId
						);
				}

				if (fee > 0)
				{
					yield return PartialActivity.CreateFee(
						Currency.EUR,
						dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
						fee,
						new Money(Currency.EUR, fee),
						transactionId
						);
				}
			}

			yield return PartialActivity.CreateKnownBalance(
				Currency.EUR,
				dateParsed.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
				balanceParsed
				);
		}

		private DescriptionData ParseDescription(IReadOnlyList<SingleWordToken> singleWordTokens)
		{
			/* Example Description:
			Processing order 572591 Gold €0.00 €110.01
			Product Gold, Zürich
			Date Submitted 22 - 05 - 2023 16:50:27
			Execution Date 22 - 05 - 2023 16:50:27
			Action Buy
			Transaction Value €0.00
			Fee €0.00
			Volume 1.000
			Total €0.00
			*/

			var result = new DescriptionData();

			// Group tokens by row to handle multi-line descriptions
			var rows = PdfTableExtractor.GroupRows(singleWordTokens);

			foreach (var row in rows)
			{
				var rowText = row.Text;

				if (rowText.StartsWith("Execution Date", StringComparison.InvariantCultureIgnoreCase))
				{
					var dateString = ExtractValueFromLine(rowText, "Execution Date");
					if (!string.IsNullOrEmpty(dateString) && TryParseDateTime(dateString, out var executionDate))
					{
						result.ExecutionDate = executionDate;
					}
				}
				else if (rowText.StartsWith("Action", StringComparison.InvariantCultureIgnoreCase))
				{
					result.Action = ExtractValueFromLine(rowText, "Action");
				}
				else if (rowText.StartsWith("Transaction Value", StringComparison.InvariantCultureIgnoreCase))
				{
					var valueString = ExtractValueFromLine(rowText, "Transaction Value");
					result.TransactionValue = ParseDecimalFromLine(valueString);
				}
				else if (rowText.StartsWith("Fee", StringComparison.InvariantCultureIgnoreCase))
				{
					var feeString = ExtractValueFromLine(rowText, "Fee");
					result.Fee = ParseDecimalFromLine(feeString);
				}
				else if (rowText.StartsWith("Volume", StringComparison.InvariantCultureIgnoreCase))
				{
					var volumeString = ExtractValueFromLine(rowText, "Volume");
					result.Volume = ParseDecimalFromLine(volumeString);
				}
				else if (rowText.StartsWith("Total", StringComparison.InvariantCultureIgnoreCase))
				{
					var totalString = ExtractValueFromLine(rowText, "Total");
					result.Total = ParseDecimalFromLine(totalString);
				}
			}

			return result;
		}

		private static string ExtractValueFromLine(string line, string prefix)
		{
			if (line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
			{
				return line.Substring(prefix.Length).Trim();
			}
			return string.Empty;
		}

		private static bool TryParseDateTime(string input, out DateTime dateTime)
		{
			dateTime = default;

			// Try to match the pattern: "22 - 05 - 2023 16:50:27"
			var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d{1,2})\s*-\s*(\d{1,2})\s*-\s*(\d{4})\s+(\d{1,2}):(\d{1,2}):(\d{1,2})");
			if (match.Success)
			{
				if (int.TryParse(match.Groups[1].Value, out var day) &&
					int.TryParse(match.Groups[2].Value, out var month) &&
					int.TryParse(match.Groups[3].Value, out var year) &&
					int.TryParse(match.Groups[4].Value, out var hour) &&
					int.TryParse(match.Groups[5].Value, out var minute) &&
					int.TryParse(match.Groups[6].Value, out var second))
				{
					try
					{
						dateTime = new DateTime(year, month, day, hour, minute, second);
						return true;
					}
					catch
					{
						return false;
					}
				}
			}

			return false;
		}

		private static decimal ParseDecimalFromLine(string input)
		{
			// Remove currency symbols and extra spaces
			var cleaned = input.Replace("€", "").Replace("EUR", "").Trim();

			// Try to parse the decimal value
			if (decimal.TryParse(cleaned.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}

			return 0m;
		}

		private decimal ParseDecimal(string? amount)
		{
			// €0.01
			if (decimal.TryParse(
				amount?.Replace("€", "").Replace(",", "").Trim(),
				NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
				CultureInfo.InvariantCulture,
				out var parsedAmount))
			{
				return parsedAmount;
			}

			throw new FormatException($"Unable to parse decimal amount: {amount}");
		}

		private DateOnly ParseDate(string? date)
		{
			if (string.IsNullOrWhiteSpace(date))
			{
				throw new FormatException($"Unable to parse date: {date}");
			}

			if (DateOnly.TryParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
			{
				// Assume Universal, do not convert to local
				return parsedDate;
			}

			throw new FormatException($"Unable to parse date: {date}");
		}
	}
}
