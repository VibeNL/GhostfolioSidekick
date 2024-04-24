namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record SingleWordToken : WordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public SingleWordToken(string text, Position boundingBox) : base(boundingBox)
		{
			Text = text;
		}

		public string Text { get; }

	}
}
