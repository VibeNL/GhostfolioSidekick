namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record SingleWordToken : WordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public string Text { get; }
	}
}
