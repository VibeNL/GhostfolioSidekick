using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class EnglishInterestPaymentInvoiceParser : BaseSubParser
	{
		private readonly string[] InterestPayment = ["POSITION", "NOMINAL", "COUPON", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(InterestPayment, "Billing", column4, true, new ColumnAlignmentMergeStrategy()),
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

			if (row.HasHeader(EnglishBillingParser.BillingHeaders))
			{
				foreach (var activity in EnglishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(InterestPayment))
			{
				var positionColumn = row.Columns[0];
				var isin = EnglishPositionParser.ExtractIsin(positionColumn);
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
