using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class BaseSubParser : ITradeRepublicActivityParser
	{
		protected abstract string StopWord { get; }
		public abstract string[] HeaderKeywords { get; }

		public bool CanParseRecord(List<SingleWordToken> words)
		{
			return ParseRecords(words).Count != 0; // TODO, pass to the subparsers
		}

		public List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				HeaderKeywords,
				[
					new(0, ColumnAlignment.Left),   // e.g. POSITION
					new(1, ColumnAlignment.Left),   // e.g. QUANTITY  
					new(2, ColumnAlignment.Left),   // e.g. PRICE
					new(3, ColumnAlignment.Right)   // e.g. AMOUNT (right-aligned)
				],
				stopPredicate: StopPredicate,
				mergePredicate: MergePredicate);

			foreach (var row in rows.Where(x => x.Columns[0].Any()))
			{
				var parsed = ParseRecord(header, row, words);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		protected abstract IEnumerable<PartialActivity> ParseRecord(PdfTableRow header, PdfTableRowColumns row, List<SingleWordToken> words);

		protected static string GenerateHash(List<SingleWordToken> words)
		{
			var sb = new StringBuilder();
			foreach (var word in words)
			{
				sb.Append(word.Text);
				sb.Append('|');
			}

			var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
			return Convert.ToHexStringLower(hashBytes);
		}

		private static bool MergePredicate(PdfTableRow current, PdfTableRow next)
		{
			// Merge if the first column of the next row is filled (has content)
			if (next.Tokens.Count == 0)
			{
				return false;
			}

			// Get the first token of the next row
			var firstToken = next.Tokens.FirstOrDefault();
			if (firstToken?.BoundingBox == null)
			{
				return false;
			}

			// Check if we have a current row to compare against
			if (current.Tokens.Count == 0)
			{
				return false;
			}

			// Get the first token's column position from the next row
			var firstTokenColumn = firstToken.BoundingBox.Column;

			// Get the first column position from the current row for comparison
			var currentFirstColumnPosition = current.Tokens
				.Where(t => t.BoundingBox != null)
				.FirstOrDefault()?.BoundingBox?.Column;

			if (!currentFirstColumnPosition.HasValue)
			{
				return false;
			}

			// Check if the first token of the next row aligns with the first column position
			// If it does, it means the first column is filled and we should merge
			var distanceToFirstColumn = Math.Abs(firstTokenColumn - currentFirstColumnPosition.Value);

			// Use a small tolerance for alignment (tokens might not be perfectly aligned)
			const int alignmentTolerance = 10;

			// Merge if the first token of next row is aligned with the first column
			return distanceToFirstColumn <= alignmentTolerance;
		}

		private bool StopPredicate(PdfTableRow row) => row.Text.Contains(StopWord, StringComparison.InvariantCultureIgnoreCase);
	}

	public class InvoiceEnglish : BaseSubParser
	{
		public override string[] HeaderKeywords => ["POSITION", "QUANTITY", "PRICE", "AMOUNT"];

		protected override string StopWord => "BOOKING";

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRow header, PdfTableRowColumns row, List<SingleWordToken> words)
		{
			var (type, date) = DetermineTypeAndDate(words);
			if (type == PartialActivityType.Undefined)
			{
				yield break;
			}

			var positionColumn = row.Columns[0];
			var positionPerLine = positionColumn.GroupBy(x => x.BoundingBox?.Row);
			var isin = positionPerLine
				.Select(g => string.Join(" ", g.OrderBy(t => t.BoundingBox?.Column).Select(t => t.Text)))
				.FirstOrDefault(line => line.StartsWith("ISIN:"))
				?.Replace("ISIN:", "").Trim() ?? string.Empty;
			var quantity = row.Columns[1][0].Text;
			var price = row.Columns[2][0].Text;
			var amount = row.Columns[3][0].Text;
			var currency = Currency.GetCurrency(row.Columns[3][1].Text);

			var transactionId = $"Trade_Republic_{isin}_{date:yyyy-MM-dd-HH-mm}";

			if (type == PartialActivityType.Buy)
			{
				yield return PartialActivity.CreateBuy(
					currency,
					date,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					ParseDecimal(quantity),
					new Money(currency, ParseDecimal(price)),
					new Money(currency, ParseDecimal(amount)),
					transactionId
				);
			}
			else if (type == PartialActivityType.Sell)
			{
				// Handle Sell activity
			}
		}

		private decimal ParseDecimal(string x)
		{
			if (decimal.TryParse(x, NumberStyles.Currency, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}

			throw new FormatException($"Unable to parse '{x}' as decimal.");
		}

		// Search for words "Market-Order Buy on"
		// Note that the words are multiple tokens, so we need to search for the sequence
		private static (PartialActivityType, DateTime) DetermineTypeAndDate(List<SingleWordToken> words)
		{
			for (int i = 0; i < words.Count - 3; i++)
			{
				if (words[i].Text.Equals("Market-Order", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 1].Text.Equals("Buy", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 2].Text.Equals("on", StringComparison.InvariantCultureIgnoreCase))
				{
					// Market-Order Buy on 06.10.2023 at 17:12
					var parseDate = words[i + 3].Text + " " + words[i + 5].Text;
					return (PartialActivityType.Buy, GetDateTime(parseDate));
				}
				else if (words[i].Text.Equals("Market-Order", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 1].Text.Equals("Sell", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 2].Text.Equals("on", StringComparison.InvariantCultureIgnoreCase))
				{
					// Market-Order Sell on 06.10.2023 at 17:12
					var parseDate = words[i + 3].Text + " " + words[i + 5].Text;
					return (PartialActivityType.Sell, GetDateTime(parseDate));
				}
			}

			return (PartialActivityType.Undefined, DateTime.Now);

			static DateTime GetDateTime(string parseDate)
			{
				var dateTime = DateTime.ParseExact(parseDate, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None);
				dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
				return dateTime;
			}
		}
	}
}
