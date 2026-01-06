using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class EnglishStockInvoiceParser : BaseSubParser
	{
		private readonly string[] Stock = ["POSITION", "QUANTITY", "PRICE", "AMOUNT"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		
		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;
		
		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(Stock, "BOOKING", column4, true, new ColumnAlignmentMergeStrategy()), // Stock table is required
					EnglishBillingParser.CreateBillingTableDefinition(isRequired: false) // Billing is optional
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

			if (row.HasHeader(EnglishBillingParser.BillingHeaders))
			{
				foreach (var activity in EnglishBillingParser.ParseBillingRecord(row, date, transactionId, ParseDecimal))
				{
					yield return activity;
				}
			}
			else if (row.HasHeader(Stock))
			{
				var positionColumn = row.Columns[0];
				var isin = EnglishPositionParser.ExtractIsin(positionColumn);
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
}
