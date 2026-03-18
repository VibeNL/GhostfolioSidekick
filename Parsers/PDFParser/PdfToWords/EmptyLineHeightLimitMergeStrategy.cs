namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Merge strategy that merges rows when the difference between their row indices
	/// is less than or equal to a fixed row skip threshold.
	/// </summary>
	public class EmptyLineHeightLimitMergeStrategy : IMergeRowStrategy
	{
		private const int RowSkip = 10;

		public bool ShouldMerge(PdfTableRow current, PdfTableRow next)
		{
			if (next.Row - current.Row <= RowSkip)
			{
				return true;
			}

			return false;
		}
	}
}