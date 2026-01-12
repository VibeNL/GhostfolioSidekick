namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Merge strategy that always merges consecutive rows.
	/// Useful for tables where all rows should be treated as continuation of the first row.
	/// </summary>
	public class AlwaysMergeStrategy : IMergeRowStrategy
	{
		public bool ShouldMerge(PdfTableRow current, PdfTableRow next)
		{
			// Always merge as long as both rows have content
			return current.Tokens.Count > 0 && next.Tokens.Count > 0;
		}
	}
}