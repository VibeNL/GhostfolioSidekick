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
			var isinLine = positionPerLine
				.Select(g => string.Join(" ", g.OrderBy(t => t.BoundingBox?.Column).Select(t => t.Text)))
				.FirstOrDefault(line => line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase) || IsIsin(line));

			if (isinLine != null && isinLine.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
			{
				var value = isinLine.Substring("ISIN:".Length).Trim();
				return IsIsin(value) ? value : string.Empty;
			}
			return isinLine != null && IsIsin(isinLine.Trim()) ? isinLine.Trim() : string.Empty;
		}

		public static string ExtractIsin(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return string.Empty;
			}

			if (line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
			{
				var value = line.Substring("ISIN:".Length).Trim();
				return IsIsin(value) ? value : string.Empty;
			}

			return IsIsin(line.Trim()) ? line.Trim() : string.Empty;
		}

		public static string ExtractIsinMultistring(string descriptionString)
		{
			if (string.IsNullOrWhiteSpace(descriptionString))
			{
				return string.Empty;
			}

			var lines = descriptionString.Split([' '], StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var isin = ExtractIsin(line);
				if (!string.IsNullOrEmpty(isin))
				{
					return isin;
				}
			}

			return string.Empty;
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