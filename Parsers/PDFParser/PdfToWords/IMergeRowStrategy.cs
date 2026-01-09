namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Strategy interface for determining how to merge table rows in PDF parsing.
	/// </summary>
	public interface IMergeRowStrategy
	{
		/// <summary>
		/// Determines whether two consecutive PDF table rows should be merged.
		/// </summary>
		/// <param name="current">The current row being processed</param>
		/// <param name="next">The next row to potentially merge with current</param>
		/// <returns>True if rows should be merged, false otherwise</returns>
		bool ShouldMerge(PdfTableRow current, PdfTableRow next);
	}
}