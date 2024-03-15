namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record Token
	{
		public Token(string text)
		{
			Text = text;
		}

		public string Text { get; }
	}
}
