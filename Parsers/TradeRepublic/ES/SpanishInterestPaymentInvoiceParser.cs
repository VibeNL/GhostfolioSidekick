using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.ES
{
	public class SpanishInterestPaymentInvoiceParser : BaseSubParser
	{
		// Original text: "POSICIÓN", "NOMINALES", "CUPÓN", "CANTIDAD"
		private readonly string[] InterestPayment = ["POSICI\u00d3N", "NOMINALES", "CUP\u00d3N", "CANTIDAD"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DateTokens => ["FECHA"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(InterestPayment, "TOTAL", column4, true, new AlwaysMergeStrategy()),
					// No billing table as it contains identical information
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["Pago", "de", "Intereses", "con"]) 
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

			if (row.HasHeader(SpanishBillingParser.BillingHeaders))
			{
				foreach (var activity in SpanishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(InterestPayment))
			{
				var positionColumn = row.Columns[0];
				var isin = ISINParser.ExtractIsin(positionColumn);
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
}
