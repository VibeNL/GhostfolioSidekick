namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record WordToken
	{
		public WordToken()
		{
		}

		public WordToken(Position? boundingBox)
		{
			BoundingBox = boundingBox;
		}

		public Position? BoundingBox { get; }
	}
}