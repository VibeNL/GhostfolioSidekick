namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record SingleWordToken : WordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public SingleWordToken(string text, BoundingBox boundingBox) : base( boundingBox)
		{
			Text = text;
		}

		public string Text { get; }

	}
}
