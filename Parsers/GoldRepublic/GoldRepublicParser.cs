using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.GoldRepublic
{
	public partial class GoldRepublicParser(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		private static readonly string[] HeaderKeywords = ["Transaction Type", "Date", "Description", "Bullion", "Amount", "Balance"];

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
			var description = row.GetColumnValue(header, "Description")?.Trim();
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
					dateParsed.ToDateTime(TimeOnly.MinValue),
					amountParsed,
					new Money(Currency.EUR, amountParsed),
					transactionId
					);
			}

			if (transactionType.Equals("Withdrawal", StringComparison.InvariantCultureIgnoreCase))
			{
				yield return PartialActivity.CreateCashWithdrawal(
					Currency.EUR,
					dateParsed.ToDateTime(TimeOnly.MinValue),
					amountParsed,
					new Money(Currency.EUR, amountParsed),
					transactionId
					);
			}

			if (transactionType.Equals("Market Order", StringComparison.InvariantCultureIgnoreCase) ||
				transactionType.Equals("Savings Order", StringComparison.InvariantCultureIgnoreCase))
			{
				var subTable = ParseDescription(description);
				var executionDate = subTable.ExecutionDate;
				var action = subTable.Action;
				var transactionValue = subTable.TransactionValue;
				var fee = subTable.Fee;
				var volume = subTable.Volume;
				var total = subTable.Total;

				yield return PartialActivity.CreateBuy(
					Currency.EUR,
					dateParsed.ToDateTime(TimeOnly.MinValue),
					[PartialSymbolIdentifier.CreateGeneric(bullion ?? "<??>")],
					volume,
					new Money(Currency.EUR, transactionValue),
					new Money(Currency.EUR, total),
					transactionId
					);

				if (fee > 0)
				{
					yield return PartialActivity.CreateFee(
						Currency.EUR,
						dateParsed.ToDateTime(TimeOnly.MinValue),
						fee,
						new Money(Currency.EUR, fee),
						transactionId
						);
				}
			}

			yield return PartialActivity.CreateKnownBalance(
				Currency.EUR,
				dateParsed.ToDateTime(TimeOnly.MinValue),
				balanceParsed
				);
		}

		private GoldRepublicTransactionDetails ParseDescription(string? description)
		{
			if (string.IsNullOrWhiteSpace(description))
			{
				return new GoldRepublicTransactionDetails(null, string.Empty, 0m, 0m, 0m, 0m);
			}

			// Initialize default values
			DateOnly? executionDate = null;
			string action = string.Empty;
			decimal transactionValue = 0m;
			decimal fee = 0m;
			decimal volume = 0m;
			decimal total = 0m;

			// Split description into lines for parsing
			var lines = description.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			
			foreach (var line in lines)
			{
				var cleanLine = line.Trim();

				// Parse execution date (format could be "Executed: dd-MM-yyyy" or similar)
				if (cleanLine.Contains("Executed:", StringComparison.InvariantCultureIgnoreCase) ||
					cleanLine.Contains("Execution Date:", StringComparison.InvariantCultureIgnoreCase))
				{
					var datePart = cleanLine.Split(':')[^1].Trim();
					if (DateOnly.TryParseExact(datePart, "dd-MM-yyyy", null, DateTimeStyles.None, out var parsedDate))
					{
						executionDate = parsedDate;
					}
				}

				// Parse action (Buy/Sell)
				if (cleanLine.Contains("Action:", StringComparison.InvariantCultureIgnoreCase))
				{
					action = cleanLine.Split(':')[^1].Trim();
				}
				else if (cleanLine.StartsWith("Buy", StringComparison.InvariantCultureIgnoreCase) ||
						 cleanLine.StartsWith("Sell", StringComparison.InvariantCultureIgnoreCase))
				{
					action = cleanLine.Split(' ')[0];
				}

				// Parse monetary values
				if (cleanLine.Contains("Transaction Value:", StringComparison.InvariantCultureIgnoreCase))
				{
					var valuePart = cleanLine.Split(':')[^1].Trim();
					transactionValue = ParseDecimalFromLine(valuePart);
				}

				if (cleanLine.Contains("Fee:", StringComparison.InvariantCultureIgnoreCase))
				{
					var feePart = cleanLine.Split(':')[^1].Trim();
					fee = ParseDecimalFromLine(feePart);
				}

				if (cleanLine.Contains("Volume:", StringComparison.InvariantCultureIgnoreCase))
				{
					var volumePart = cleanLine.Split(':')[^1].Trim();
					volume = ParseDecimalFromLine(volumePart);
				}

				if (cleanLine.Contains("Total:", StringComparison.InvariantCultureIgnoreCase))
				{
					var totalPart = cleanLine.Split(':')[^1].Trim();
					total = ParseDecimalFromLine(totalPart);
				}

				// Try to extract values from patterns like "€123.45"
				if (string.IsNullOrEmpty(action))
				{
					if (cleanLine.Contains("€") && (cleanLine.Contains("gram", StringComparison.InvariantCultureIgnoreCase) || cleanLine.Contains("g ", StringComparison.InvariantCultureIgnoreCase)))
					{
						// This might be a transaction line, assume it's a buy if not specified
						action = "Buy";
					}
				}
			}

			// If we couldn't parse structured data, try to extract from the entire description
			if (executionDate == null && action == string.Empty && transactionValue == 0m)
			{
				// Try to extract any decimal values and dates from the entire description
				var euroMatches = System.Text.RegularExpressions.Regex.Matches(description, @"€\s*(\d+(?:[.,]\d+)?)");
				if (euroMatches.Count > 0)
				{
					// First euro amount might be transaction value
					if (decimal.TryParse(euroMatches[0].Groups[1].Value.Replace(',', '.'), out var value))
					{
						transactionValue = value;
					}
					
					// If there are multiple euro amounts, last one might be total
					if (euroMatches.Count > 1)
					{
						if (decimal.TryParse(euroMatches[^1].Groups[1].Value.Replace(',', '.'), out var totalValue))
						{
							total = totalValue;
						}
					}
				}

				// Try to find any date in the description
				var dateMatches = System.Text.RegularExpressions.Regex.Matches(description, @"\b(\d{1,2}[-/]\d{1,2}[-/]\d{4})\b");
				if (dateMatches.Count > 0)
				{
					if (DateOnly.TryParseExact(dateMatches[0].Groups[1].Value, new[] { "dd-MM-yyyy", "dd/MM/yyyy" }, null, DateTimeStyles.None, out var parsedDate))
					{
						executionDate = parsedDate;
					}
				}

				// Default action if not found
				action = "Buy";
			}

			return new GoldRepublicTransactionDetails(executionDate, action, transactionValue, fee, volume, total);
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
			//  17-05-2023
			if (DateOnly.TryParseExact(date, "dd-MM-yyyy", null, DateTimeStyles.None, out var parsedDate))
			{
				return parsedDate;
			}

			throw new FormatException($"Unable to parse date: {date}");
		}
	}
}
