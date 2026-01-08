using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.ES
{
	public class SpanishBondRepaymentInvoiceParser : BaseSubParser
	{
		// Original text: "NÚM.", "REGISTRO", "INSTRUMENTO", "CANTIDAD"
		private readonly string[] BondRepayment = ["N\u00daM.", "REGISTRO", "INSTRUMENTO", "CANTIDAD"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		
		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;
		
		protected override string[] DateTokens => ["FECHA"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(BondRepayment, "FACTURA", column4, true, new AlwaysMergeStrategy()),
					// No billing table as it contains identical information
				];
			}
		}

		// Note: Bond repayment pattern detection needs to be defined based on actual TradeRepublic documents
		// This is a placeholder implementation
		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			ContainsSequence([.. words.Select(w => w.Text)], ["VENCIMIENTO", "FINAL"]) 
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

			if (row.HasHeader(SpanishBillingParser.BillingHeaders))
			{
				foreach (var activity in SpanishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(BondRepayment))
			{
				var isin = ISINParser.ExtractIsin(row.Columns[2]);
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
