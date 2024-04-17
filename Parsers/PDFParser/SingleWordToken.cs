namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record SingleWordToken : WordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public SingleWordToken(string text, Point topLeft, Point bottomRight)
		{
			Text = text;
			TopLeft = topLeft;
			BottomRight = bottomRight;
		}

		public string Text { get; }

		public Point? TopLeft { get; }

		public Point? BottomRight { get; }
	}
}
