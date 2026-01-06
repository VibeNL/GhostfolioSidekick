using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic.EN
{
	public static class EnglishPositionParser
	{
		public static string ExtractIsin(IReadOnlyList<SingleWordToken> positionColumn)
		{
			if (positionColumn == null || positionColumn.Count == 0)
			{
				return string.Empty;
			}

			var positionPerLine = positionColumn.GroupBy(x => x.BoundingBox?.Row);
			var isin = positionPerLine
				.Select(g => string.Join(" ", g.OrderBy(t => t.BoundingBox?.Column).Select(t => t.Text)))
				.FirstOrDefault(line => line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
				?.Replace("ISIN:", "").Trim() ?? string.Empty;

			return isin;
		}
	}
}
