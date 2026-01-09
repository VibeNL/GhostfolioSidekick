using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public class EnglishCostInformationAltParser : BaseSubParser
	{
		private readonly string[] CostInformation = ["SECURITY", "ORDER / NOMINAL", "VALUE"];
		private readonly ColumnAlignment[] column4 = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Right];
		
		protected override CultureInfo CultureInfo => CultureInfo.InvariantCulture;
		
		protected override string[] DateTokens => ["DATE"];

		protected override TableDefinition[] TableDefinitions
		{
			get
			{
				return [
					new TableDefinition(CostInformation, "COST", column4, true, new ColumnAlignmentMergeStrategy()), // Cost table is required
				];
			}
		}

		protected override IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId)
		{
			yield return PartialActivity.CreateIgnore();
		}
	}
}
