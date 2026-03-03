using System;
using System.Linq;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Merge strategy that merges rows until an empty line is encountered or a height limit is reached.
	/// </summary>
	public class EmptyLineHeightLimitMergeStrategy : IMergeRowStrategy
	{
		private readonly int RowSkip = 10;

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