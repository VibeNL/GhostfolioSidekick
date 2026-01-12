namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public sealed record PdfTableRow(string[] Headers, int Page, int Row, IReadOnlyList<SingleWordToken> Tokens)
	{
		public string Text => string.Join(" ", Tokens.Select(t => t.Text));

		internal bool IsHeader(string[] compareHeaders)
		{
			return
				compareHeaders.Length == Headers.Length &&
				compareHeaders.All(h =>
					!string.IsNullOrWhiteSpace(h) ||
					Headers.Any(rh => string.Equals(rh, h, StringComparison.InvariantCultureIgnoreCase)));
		}
	}
}
