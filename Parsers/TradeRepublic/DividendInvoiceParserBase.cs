using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class DividendInvoiceParserBase : BaseSubParser
	{
		protected abstract string[] DividendHeaders { get; }
		protected abstract ColumnAlignment[] DividendColumnAlignment { get; }
		protected abstract string[] DividendKeywords { get; }
		protected abstract IBillingParser BillingParser { get; }
		protected abstract IPositionParser PositionParser { get; }

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(DividendHeaders, "Billing", DividendColumnAlignment, true, new ColumnAlignmentMergeStrategy()),
					BillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
				];
			}
		}

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
			else if (row.HasHeader(DividendHeaders))
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

		private PartialActivityType DetermineType(List<SingleWordToken> words)
		{
			var wordTexts = words.Select(w => w.Text).ToArray();
			
			// Check for dividend sequence with ex-tag keywords
			var dividendSequence = DividendKeywords.ToArray();
			return ContainsSequence(wordTexts, dividendSequence) 
				? PartialActivityType.Dividend 
				: PartialActivityType.Undefined;
		}
	}

	public interface IBillingParser
	{
		string[] BillingHeaders { get; }
		TableDefinition CreateBillingTableDefinition(string endMarker = "TOTAL", bool isRequired = false);
		IEnumerable<PartialActivity> ParseBillingRecord(PdfTableRowColumns row, DateTime date, string transactionId, Func<string, decimal> parseDecimal);
	}

	public interface IPositionParser
	{
		string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn);
	}
}