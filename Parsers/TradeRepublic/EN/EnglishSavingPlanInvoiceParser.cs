using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public class EnglishSavingPlanInvoiceParser : BaseSubParser
	{
		private readonly string[] SavingPlan = ["POSITION", "QUANTITY", "AVERAGE RATE", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;

		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(SavingPlan, "BOOKING", column4, true, new ColumnAlignmentMergeStrategy()), // SavingPlan table is required
					EnglishBillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
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

			if (row.HasHeader(EnglishBillingParser.BillingHeaders))
			{
				foreach (var activity in EnglishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(SavingPlan))
			{
				var positionColumn = row.Columns[0];
				var isin = EnglishPositionParser.ExtractIsin(positionColumn);
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
