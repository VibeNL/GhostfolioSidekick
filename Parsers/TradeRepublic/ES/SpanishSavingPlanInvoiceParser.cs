using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.ES
{
	public class SpanishSavingPlanInvoiceParser : BaseSubParser
	{
		// Original text: "POSICIÓN", "CANTIDAD", "COTIZACIÓN PROMEDIO", "IMPORTE"
		private readonly string[] SavingPlan = ["POSICI\u00d3N", "CANTIDAD", "COTIZACI\u00d3N PROMEDIO", "IMPORTE"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override CultureInfo CultureInfo => new("es-ES");

		protected override string[] DateTokens => ["FECHA"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(SavingPlan, "RESERVA", column4, true, new ColumnAlignmentMergeStrategy()), // SavingPlan table is required
					SpanishBillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

		private static PartialActivityType DetermineType(List<SingleWordToken> words) =>
			// Original text: "Ejecución del plan de inversión el día"
			ContainsSequence([.. words.Select(w => w.Text)], ["Ejecuci\u00f3n", "del", "plan", "de", "inversi\u00f3n", "el", "d\u00eda"]) 
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

			if (row.HasHeader(SpanishBillingParser.BillingHeaders))
			{
				foreach (var activity in SpanishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(SavingPlan))
			{
				var positionColumn = row.Columns[0];
				var isin = ISINParser.ExtractIsin(positionColumn);
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
}
