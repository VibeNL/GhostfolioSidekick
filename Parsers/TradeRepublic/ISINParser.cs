using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public static class ISINParser
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
				.FirstOrDefault(line => line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase) || IsIsin(line))
				?.Replace("ISIN:", "").Trim() ?? string.Empty;

			return isin;
		}

		private static bool IsIsin(string line)
		{
			if (line.Length != 12)
			{
				return false;
			}

			// ISIN format: 2 letters, 9 alphanumeric characters, 1 digit
			if (!char.IsLetter(line[0]) || !char.IsLetter(line[1]))
			{
				return false;
			}

			for (int i = 2; i < 11; i++)
			{
				if (!char.IsLetterOrDigit(line[i]))
				{
					return false;
				}
			}

			if (!char.IsDigit(line[11]))
			{
				return false;
			}

			return true;
		}
	}
}