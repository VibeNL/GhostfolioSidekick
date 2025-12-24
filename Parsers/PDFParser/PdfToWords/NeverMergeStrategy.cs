namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Merge strategy that never merges rows.
	/// Each row is processed independently.
	/// </summary>
	public class NeverMergeStrategy : IMergeRowStrategy
	{
		public bool ShouldMerge(PdfTableRow current, PdfTableRow next)
		{
			// Never merge rows
			return false;
		}
	}
}