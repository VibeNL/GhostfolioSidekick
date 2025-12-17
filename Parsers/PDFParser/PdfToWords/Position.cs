namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record Position
	{
		public Position(int page, int row, int column)
		{
			Page = page;
			Row = row;
			Column = column;
		}

		public int Page { get; }
		public int Row { get; }
		public int Column { get; }
	}
}