using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

		protected static decimal ParseDecimal(string x)
		{
			if (decimal.TryParse(x, NumberStyles.Currency, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}

			throw new FormatException($"Unable to parse '{x}' as decimal.");
		}

		protected static DateTime GetDateTime(string parseDate)
		{
			parseDate = parseDate
				.Replace('-', '.')
				.Trim('.'); // Just in case

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

		protected static DateTime DetermineDate(List<SingleWordToken> words)
		{
			// Find the first 'DATE' token and take the next token as date
			for (int i = 0; i < words.Count - 1; i++)
			{
				if (words[i].Text.Equals("DATE", StringComparison.InvariantCultureIgnoreCase))
				{
					return GetDateTime(words[i + 1].Text);
				}
			}
			
			throw new FormatException("Unable to determine date from the document.");
		}

		protected static bool ContainsSequence(string[] wordTexts, string[] pattern)
		{
			for (int i = 0; i <= wordTexts.Length - pattern.Length; i++)
			{
				bool matches = true;
				for (int j = 0; j < pattern.Length; j++)
				{
					if (!wordTexts[i + j].Equals(pattern[j], StringComparison.InvariantCultureIgnoreCase))
					{
						matches = false;
						break;
					}
				}
				
				if (matches)
				{
					return true;
				}
			}
			
			return false;
		}
	}

	public static class BillingParser
	{
		public static readonly string[] BillingHeaders = ["POSITION", "AMOUNT"];
		public static readonly ColumnAlignment[] BillingColumnAlignment = [ColumnAlignment.Left, ColumnAlignment.Right];

		public static TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAL", bool isRequired = false)
		{
			return new TableDefinition(BillingHeaders, endMarker, BillingColumnAlignment, isRequired);
		}

		public static IEnumerable<PartialActivity> ParseBillingRecord(
			PdfTableRowColumns row,
			DateTime date,
			string transactionId,
			Func<string, decimal> parseDecimal)
		{
			if (!row.HasHeader(BillingHeaders))
			{
				yield break;
			}

			// Multi line billing (fees)
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

				var amount = parseDecimal(parts[0]) * -1;
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
	}

	public static class PositionParser
	{
		public static string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
		{
			if (positionColumn == null || positionColumn.Count == 0)
			{
				return string.Empty;
			}

			var positionPerLine = positionColumn.GroupBy(x => x.BoundingBox?.Row);
			var isin = positionPerLine
				.Select(g => string.Join(" ", g.OrderBy(t => t.BoundingBox?.Column).Select(t => t.Text)))
				.FirstOrDefault(line => line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
				?.Replace("ISIN:", "").Trim() ?? string.Empty;

			return isin;
		}
	}

	public class StockInvoiceParser : BaseSubParser
	{
		private readonly string[] Stock = ["POSITION", "QUANTITY", "PRICE", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(Stock, "BOOKING", column4, true), // Stock table is required
					BillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			new[] { 
				(new[] { "Market-Order", "Buy", "on" }, PartialActivityType.Buy),
				(new[] { "Market-Order", "Sell", "on" }, PartialActivityType.Sell)
			}.FirstOrDefault(p => ContainsSequence([.. words.Select(w => w.Text)], p.Item1)).Item2;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);
			
			if (type == PartialActivityType.Undefined)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(Stock))
			{
				var positionColumn = row.Columns[0];
				var isin = PositionParser.ExtractIsin(positionColumn);
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
					yield return PartialActivity.CreateSell(
						currency,
						date,
						[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
						ParseDecimal(quantity),
						new Money(currency, ParseDecimal(price)),
						new Money(currency, ParseDecimal(amount)),
						transactionId
					);
				}
			}
		}
	}

	public class SavingPlanInvoiceParser : BaseSubParser
	{
		private readonly string[] SavingPlan = ["POSITION", "QUANTITY", "AVERAGE RATE", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(SavingPlan, "BOOKING", column4, true), // SavingPlan table is required
					BillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["Savings", "plan", "execution", "on"]) 
				? PartialActivityType.Buy 
				: PartialActivityType.Undefined;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);
			
			if (type == PartialActivityType.Undefined)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(SavingPlan))
			{
				var positionColumn = row.Columns[0];
				var isin = PositionParser.ExtractIsin(positionColumn);
				var quantity = row.Columns[1][0].Text;
				var price = row.Columns[2][0].Text;
				var amount = row.Columns[3][0].Text;
				var currency = Currency.GetCurrency(row.Columns[3][1].Text);

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
		}
	}

	public class BondInvoiceParser : BaseSubParser
	{
		private readonly string[] Bond = ["POSITION", "NOMINAL", "PRICE", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(Bond, "Billing", column4, true), // Bond table is required
					BillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			new[] { 
				(new[] { "Market-Order", "Buy", "on" }, PartialActivityType.Buy),
				(new[] { "Market-Order", "Sell", "on" }, PartialActivityType.Sell)
			}.FirstOrDefault(p => ContainsSequence([.. words.Select(w => w.Text)], p.Item1)).Item2;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);
			
			if (type == PartialActivityType.Undefined)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(Bond))
			{
				var positionColumn = row.Columns[0];
				var isin = PositionParser.ExtractIsin(positionColumn);
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
					yield return PartialActivity.CreateSell(
						currency,
					 date,
					 [PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					 ParseDecimal(quantity),
					 new Money(currency, ParseDecimal(price)),
					 new Money(currency, ParseDecimal(amount)),
					 transactionId
				  );
				}
			}
		}
	}

	public class DividendInvoiceParser : BaseSubParser
	{
		private readonly string[] Dividend = ["POSITION", "QUANTITY", "INCOME", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(Dividend, "Billing", column4, true), // Dividend table is required
					BillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["Dividend", "with", "the", "ex-tag"]) 
				? PartialActivityType.Dividend 
				: PartialActivityType.Undefined;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);
			
			if (type != PartialActivityType.Dividend)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(Dividend))
			{
				var positionColumn = row.Columns[0];
				var isin = PositionParser.ExtractIsin(positionColumn);
				var amount = row.Columns[3][0].Text;
				var currency = Currency.GetCurrency(row.Columns[3][1].Text);
				
				yield return PartialActivity.CreateDividend(
					currency,
					date,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					ParseDecimal(amount),
					new Money(currency, ParseDecimal(amount)),
					transactionId
				);
			}
		}
	}

	public class InterestPaymentInvoiceParser : BaseSubParser
	{
		private readonly string[] InterestPayment = ["POSITION", "NOMINAL", "COUPON", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(InterestPayment, "Billing", column4, true), // InterestPayment table is required
					// No billing table as it contains identical information
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["Interest", "Payment", "with", "the"]) 
				? PartialActivityType.Dividend 
				: PartialActivityType.Undefined;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);
			
			if (type != PartialActivityType.Dividend)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(InterestPayment))
			{
				var positionColumn = row.Columns[0];
				var isin = PositionParser.ExtractIsin(positionColumn);
				var amount = row.Columns[3][0].Text;
				var currency = Currency.GetCurrency(row.Columns[3][1].Text);
				
				yield return PartialActivity.CreateDividend(
					currency,
					date,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					ParseDecimal(amount),
					new Money(currency, ParseDecimal(amount)),
					transactionId
				);
			}
		}
	}

	public class BondRepaymentInvoiceParser : BaseSubParser
	{
		private readonly string[] BondRepayment = ["NO", "BOOKING", "SECURITY", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(BondRepayment, "Billing", column4, true), // BondRepayment table is required
					// No billing table as it contains identical information
				];
			}
		}

		// Note: Bond repayment pattern detection needs to be defined based on actual TradeRepublic documents
		// This is a placeholder implementation
		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["Repayment"]) 
				? PartialActivityType.BondRepay 
				: PartialActivityType.Undefined;

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			var date = DetermineDate(words);
			var type = DetermineType(words);

			if (type != PartialActivityType.BondRepay)
			{
				yield break;
			}

			if (row.HasHeader(BillingParser.BillingHeaders))
			{
				foreach (var activity in BillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(BondRepayment))
			{
				var positionColumn = row.Columns[2];
				var isin = PositionParser.ExtractIsin(positionColumn);
				var amount = row.Columns[3][0].Text;
				var currency = Currency.GetCurrency(row.Columns[3][1].Text);

				yield return PartialActivity.CreateBondRepay(
					currency,
					date,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					new Money(currency, ParseDecimal(amount)),
					new Money(currency, ParseDecimal(amount)),
					transactionId
				);
			}
		}
	}
}
