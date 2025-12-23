using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.ISIN;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{

	public abstract class BaseSubParser : ITradeRepublicActivityParser
	{
		protected abstract TableDefinition[] TableDefinitions { get; }

		public bool CanParseRecord(string filename, List<SingleWordToken> words)
		{
			return ParseRecords(filename, words).Count != 0; // TODO, pass to the subparsers
		}

		public List<PartialActivity> ParseRecords(string filename, List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			var rows = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				TableDefinitions,
				mergePredicate: MergePredicate);

			var transactionId = $"Trade_Republic_{Path.GetFileName(filename)}";

			foreach (var row in rows.Where(x => x.Columns[0].Any()))
			{
				var parsed = ParseRecord(row, words, transactionId);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		protected abstract IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId);

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

		protected decimal ParseDecimal(string x)
		{
			if (decimal.TryParse(x, NumberStyles.Currency, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}

			throw new FormatException($"Unable to parse '{x}' as decimal.");
		}

		// Date format: 06.10.2023 17:12
		// Or without time: 06.10.2023
		protected DateTime GetDateTime(string parseDate)
		{
			if (DateTime.TryParseExact(parseDate, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
			{
				dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
				return dateTime;
			}

			if (DateTime.TryParseExact(parseDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
			{
				dateOnly = DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
				return dateOnly;
			}

			throw new FormatException($"Unable to parse '{parseDate}' as DateTime.");
		}
	}

	public class InvoiceStockEnglish : BaseSubParser
	{
		private readonly string[] HeaderStockWithoutFee = ["POSITION", "QUANTITY", "PRICE", "AMOUNT"];
		private readonly string[] SavingPlanWithoutFee = ["POSITION", "QUANTITY", "AVERAGE RATE", "AMOUNT"];
		private readonly string[] BondWithFee = ["POSITION", "NOMINAL", "PRICE", "AMOUNT"];
		private readonly string[] Billing = ["POSITION", "AMOUNT"];

		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		private readonly ColumnAlignment[] column2 = [ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(HeaderStockWithoutFee,"BOOKING", column4), // Stock without fee
					new TableDefinition(SavingPlanWithoutFee, "BOOKING", column4), // Savings plan without fee
					new TableDefinition(BondWithFee, "Billing", column4), // Bond with fee
					new TableDefinition(Billing, "TOTAL", column2), // Fee only
			];
			}
		}

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var (type, date) = DetermineTypeAndDate(words);
			if (type == PartialActivityType.Undefined)
			{
				yield break;
			}

			// Multi line billing (fees)
			if (row.HasHeader(Billing))
			{
				// 0 is description, 1 is amount. Get by line
				var lineNumbers = row.Columns[0]
					.GroupBy(x => x.BoundingBox?.Row);

				foreach (var item in lineNumbers)
				{
					var description = row.Columns[0]
						.Where(x => x.BoundingBox?.Row == item.Key)
						.OrderBy(t => t.BoundingBox?.Column)
						.Select(t => t.Text)
						.Aggregate((current, next) => current + " " + next);
					var amountWithCurrency = row.Columns[1]
						.Where(x => x.BoundingBox?.Row == item.Key)
						.OrderBy(t => t.BoundingBox?.Column)
						.Select(t => t.Text)
						.Aggregate((current, next) => current + " " + next);

					// Split amount and currency
					var parts = amountWithCurrency.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2)
					{
						throw new FormatException($"Unable to parse amount and currency from '{amountWithCurrency}'.");
					}

					var amount = ParseDecimal(parts[0]) * -1;
					var currency = Currency.GetCurrency(parts[1]);

					yield return PartialActivity.CreateFee(
						currency,
						date,
						amount,
						new Money(currency, amount),
						transactionId
					);
				}
			}
			// Buy is always a single line
			else if (row.HasHeader(HeaderStockWithoutFee) || row.HasHeader(BondWithFee)) // TODO Implement Bonds correcly
			{
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
		}

		// Search for words "Market-Order Buy on" or Savings plan execution on
		// Note that the words are multiple tokens, so we need to search for the sequence
		private (PartialActivityType, DateTime) DetermineTypeAndDate(List<SingleWordToken> words)
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
				else if (words[i].Text.Equals("Savings", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 1].Text.Equals("plan", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 2].Text.Equals("execution", StringComparison.InvariantCultureIgnoreCase) &&
					words[i + 3].Text.Equals("on", StringComparison.InvariantCultureIgnoreCase))
				{
					// Savings plan execution on 06.10.2023 at 17:12
					var parseDate = words[i + 4].Text;
					return (PartialActivityType.Buy, GetDateTime(parseDate));
				}
			}

			return (PartialActivityType.Undefined, DateTime.Now);
		}
	}
}
