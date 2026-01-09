using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public class EnglishSettlementSavebackParser : BaseSubParser
	{
		private readonly string[] CostInformation = ["PRODUCT", "DATE", "TOTAL"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		
		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;
		
		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(CostInformation, "Total", column4, true, new ColumnAlignmentMergeStrategy()), // Cost table is required
				];
			}
		}

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			yield return PartialActivity.CreateIgnore();
		}
	}
}
