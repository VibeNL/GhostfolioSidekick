using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Utilities;

namespace GhostfolioSidekick.Parsers.TradeRepublic;

/// <summary>
/// PDF-specific ISIN extraction helpers. Kept in Parsers to avoid circular dependency on SingleWordToken.
/// </summary>
public static class ISINParserPdfExtensions
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
			.FirstOrDefault(line => line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase) || ISINParser.IsIsin(line));

		if (isinLine != null && isinLine.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
		{
			var value = isinLine.Substring("ISIN:".Length).Trim();
			return ISINParser.IsIsin(value) ? value : string.Empty;
		}
		return isinLine != null && ISINParser.IsIsin(isinLine.Trim()) ? isinLine.Trim() : string.Empty;
	}
}
