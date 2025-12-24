using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public static class BillingParserBase
	{
		public static TableDefinition CreateBillingTableDefinition(
			string[] headers, 
			ColumnAlignment[] columnAlignment,
			string endMarker,
			bool isRequired = false)
		{
			return new TableDefinition(
				headers,
				endMarker,
				columnAlignment,
				isRequired,
				new NeverMergeStrategy());
		}

		public static IEnumerable<PartialActivity> ParseBillingRecord(
			PdfTableRowColumns row,
			string[] expectedHeaders,
			DateTime date,
			string transactionId,
			Func<string, decimal> parseDecimal,
			string subtotalKeyword)
		{
			if (!row.HasHeader(expectedHeaders))
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

				if (description.Contains(subtotalKeyword))
				{
					// Skip dividends here
					continue;
				}

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
}