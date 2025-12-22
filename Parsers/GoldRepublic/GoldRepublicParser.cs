using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.GoldRepublic
{
	public partial class GoldRepublicParser(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		private static readonly string[] HeaderKeywords = ["Transaction Type", "Date", "Description", "Bullion", "Amount", "Balance"];

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			try
			{
				bool hasGoldRepublic = ContainsSequence(["WWW.GOLDREPUBLIC.COM"], words);
				bool hasAccountStatement = ContainsSequence(["Account", "Statement"], words);

				return hasGoldRepublic && hasAccountStatement;
			}
			catch
			{
				return false;
			}
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			bool StopPredicate(PdfTableRow row) => row.Text.Contains("Closing balance", StringComparison.InvariantCultureIgnoreCase);

			var (_, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				HeaderKeywords,
				stopPredicate: StopPredicate,
				mergePredicate: null);

			foreach (var row in rows)
			{
				var recordTokens = row.Columns.SelectMany(c => c).ToList();

				var parsed = ParseRecord(recordTokens);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		private IEnumerable<PartialActivity>? ParseRecord(List<SingleWordToken> recordTokens)
		{
			// TODO: Implement actual GoldRepublic activity mapping once transaction semantics are defined.
			return Enumerable.Empty<PartialActivity>();
		}
	}
}
